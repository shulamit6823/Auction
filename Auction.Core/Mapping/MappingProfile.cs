using AutoMapper;

namespace Auction.Core.Mapping;

using Auction.Core.DTOs;
using Auction.Core.Entities;
using Auction.Core.Models;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<User, UserResponse>();
        CreateMap<Auction, AuctionResponse>();
        CreateMap<Bid, BidResponse>();
        CreateMap<LoginResult, LoginResponse>();
        CreateMap<UserBidSummary, MyBidResponse>();
    }
}



