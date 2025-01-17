namespace LanGeng.API.Dtos;

public record class ResponseError<T>(string Message, T? Errors)
{
    public ResponseError(string Message) : this(Message, default)
    {
        this.Message = Message;
    }
}
