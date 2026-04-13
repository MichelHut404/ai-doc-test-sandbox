namespace documentationAutomationv1.Presentation.Samples
{
    public interface ISampleService
    {
        object GetSample(int id);

        int CreateSample(object data);

        bool DeleteSample(int id);
    }
}