namespace KBMS.Network;

public enum MessageType : byte
{
    LOGIN = 1,
    QUERY = 2,
    RESULT = 3,
    ERROR = 4,
    LOGOUT = 5
}
