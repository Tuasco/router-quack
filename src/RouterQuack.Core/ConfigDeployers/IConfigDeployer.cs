namespace RouterQuack.Core.ConfigDeployers;

/// <summary>
/// Deployer that uploads and applies generated configurations to target platforms.
/// </summary>
public interface IConfigDeployer : IStep
{
    /// <summary>
    /// Deploy generated configurations to the target platform.
    /// </summary>
    /// <param name="configDirectory">Directory containing the generated configuration files.</param>
    void DeployConfigs(string configDirectory);
}