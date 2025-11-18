namespace DocumentManagementSystem.Infrastructure.Services
{
    public interface IRabbitMqService
    {
        void SendOcrMessage(object message);
    }
}
