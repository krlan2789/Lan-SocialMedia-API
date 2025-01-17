namespace LanGeng.API.Dtos;

public record class ResponseDataArray<T>(string Message, string? Token, T[]? Data)
{
    public ResponseDataArray(string Message, T[] Data) : this(Message, default, Data)
    {
        this.Message = Message;
        this.Data = Data;
    }
}