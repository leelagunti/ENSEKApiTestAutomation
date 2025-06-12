using Reqnroll;
using NLog;

[Binding]
public class Hooks
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

    [BeforeScenario]
    public void BeforeScenario()
    {
        Logger.Info($"🔹 Starting Scenario: {ScenarioContext.Current.ScenarioInfo.Title}");
    }

    [AfterScenario]
    public void AfterScenario()
    {
        Logger.Info($"✅ Finished Scenario: {ScenarioContext.Current.ScenarioInfo.Title}");
    }
}
