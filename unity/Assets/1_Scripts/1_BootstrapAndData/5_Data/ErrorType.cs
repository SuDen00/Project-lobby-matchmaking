public enum ErrorType
{
    ConnectionFailed,
    LobbyFull,
    AlreadyInLobby,
    Timeout,
    HostExit,
    Kicked,
    LobbyCreationFailed,
    ConnectionLost,
    InvalidIP,
    LobbyClosed,
    LobbyNotFound,
    HostMissing,
    AlreadyConnected,
    UnauthorizedAction,
    AccessDenied,         // k_EChatRoomEnterResponseNotAllowed
    Banned,               // k_EChatRoomEnterResponseBanned
    CommunityBanned,      // k_EChatRoomEnterResponseCommunityBan
    MemberBlocked,        // k_EChatRoomEnterResponseMemberBlockedYou
    YouBlockedMember,     // k_EChatRoomEnterResponseYouBlockedMember
    RateLimitExceeded,    // k_EChatRoomEnterResponseRatelimitExceeded
    LimitedAccount,       // k_EChatRoomEnterResponseLimited
    ClanDisabled,         // k_EChatRoomEnterResponseClanDisabled
    GenericJoinError,
    Unknown
}