using Mirror;

public struct ShutdownMessage : NetworkMessage
{
    public DisconnectReason Reason;
}


public enum DisconnectReason
{
    None = 0,
    ServerShutdown,  // сервер штатно закрылся (прислал ShutdownMessage)
    ManualLeft,      // игрок сам вышел (left)
    Disconnected,    // сетевой обрыв
    Kicked,          // кик
    Banned           // бан
}
