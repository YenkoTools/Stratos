namespace Server;

public class EnvironmentState
{
    private static readonly string[] ValidEnvironments = ["dev", "qa", "stage", "prod"];

    public string Current { get; private set; } = "dev";

    public bool TrySet(string env)
    {
        if (!ValidEnvironments.Contains(env.ToLowerInvariant()))
            return false;

        Current = env.ToLowerInvariant();
        return true;
    }
}
