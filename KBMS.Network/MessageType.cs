namespace KBMS.Network;

public enum MessageType : byte
{
    LOGIN = 1,
    QUERY = 2,
    RESULT = 3,
    ERROR = 4,
    LOGOUT = 5,
    METADATA = 6,
    ROW = 7,
    FETCH_DONE = 8,
    STATS = 10,
    LOGS_STREAM = 11,
    SESSIONS = 12,
    MANAGEMENT_CMD = 13
}
