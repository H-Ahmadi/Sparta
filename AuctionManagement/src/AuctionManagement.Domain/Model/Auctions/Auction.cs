using System;
using AuctionManagement.Domain.Contracts.Auctions;
using AuctionManagement.Domain.Model.Auctions.Exceptions;
using AuctionManagement.Domain.Model.Participants;
using Sparta.Domain;

namespace AuctionManagement.Domain.Model.Auctions
{
    public partial class Auction : EventSourcedAggregateRoot<Guid>
    {
        public int SellerId { get; private set; }
        public SellingProduct Product { get; private set; }
        public int StartingPrice { get; private set; }
        public DateTime EndDateTime { get; private set; }
        public Bid WinningBid { get; private set; }
        protected Auction(){}
        public Auction(Participant seller, SellingProduct product, int startingPrice, DateTime endDateTime, IClock clock)
        {
            if (startingPrice <= 0) throw new InvalidStartingPriceException();
            if (endDateTime <= clock.Now()) throw new PastEndDateException();

            Causes(new AuctionOpened(Guid.NewGuid(), seller.Id, product.CategoryId, product.Name, startingPrice, endDateTime));
        }
        public void PlaceBid(Bid bid, IClock clock)
        {
            GuardAgainstClosedAuction(clock);
            GuardAgainstInvalidBidAmount(bid.Amount);
            GuardAgainstInvalidBidder(bid.BidderId);

            Causes(new BidPlaced(this.Id, bid.Amount, bid.OfferDateTime, bid.BidderId));
        }
        private void GuardAgainstClosedAuction(IClock clock)
        {
            if (this.IsClosed(clock)) throw new InvalidAuctionStateException();
        }
        private bool IsClosed(IClock clock)
        {
            return this.EndDateTime < clock.Now();
        }
        private void GuardAgainstInvalidBidAmount(int bidAmount)
        {
            var maxOffer = GetMaxOffer();
            if (bidAmount <= maxOffer) throw new InvalidBidException();
        }
        private int GetMaxOffer()
        {
            if (IsFirstOffer())
                return this.StartingPrice;

            return this.WinningBid.Amount;
        }
        private bool IsFirstOffer()
        {
            return this.WinningBid == null;
        }
        private void GuardAgainstInvalidBidder(int bidderId)
        {
            if (this.SellerId == bidderId) throw new BidderSameAsSellerException();
        }
      
    }
}