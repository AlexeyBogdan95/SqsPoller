namespace SqsPoller.Tests.Unit
{
    public interface IFakeService
    {
        void FirstMethod(FirstMessage message);
        void FirstMethod(FirstCompressedMessage message);
        void SecondMethod(SecondMessage message);
        void SecondMethod(SecondCompressedMessage message);
    }
}