namespace SqsPoller.Tests.Unit
{
    public interface IFakeService
    {
        void FirstMethod(string value);
        void SecondMethod(string value);
        void EnumMethod(SampleEnum value);
    }
}