using System.Text;
using Renci.SshNet;

namespace HealthChecks.Network;

public class SftpConfigurationBuilder
{
    private readonly string _host;
    private readonly int _port;
    private readonly string _userName;
    private (bool createFile, string remotePath) _fileCreationOptions = (false, string.Empty);

    internal List<AuthenticationMethod> AuthenticationMethods { get; } = new();
    public SftpConfigurationBuilder(string host, int port, string userName)
    {
        _host = Guard.ThrowIfNull(host);
        _userName = Guard.ThrowIfNull(userName);

        if (port == default)
            port = 22;
        _port = port;
    }
    public SftpConfigurationBuilder AddPasswordAuthentication(string password)
    {
        AuthenticationMethods.Add(new PasswordAuthenticationMethod(_userName, password));

        return this;
    }
    public SftpConfigurationBuilder AddPrivateKeyAuthentication(string privateKey, string passphrase)
    {
        Guard.ThrowIfNull(privateKey, true);
        Guard.ThrowIfNull(passphrase, true);

        var keyBytes = Encoding.UTF8.GetBytes(privateKey);

        using var memoryStream = new MemoryStream();

        memoryStream.Write(keyBytes, 0, keyBytes.Length);
        memoryStream.Seek(0, SeekOrigin.Begin);

        var privateKeyFile = new PrivateKeyFile(memoryStream, passphrase);

        AuthenticationMethods.Add(new PrivateKeyAuthenticationMethod(_userName, privateKeyFile));

        return this;
    }

    public SftpConfigurationBuilder AddPrivateKeyAuthentication(PrivateKeyFile privateKey)
    {
        AuthenticationMethods.Add(new PrivateKeyAuthenticationMethod(_userName, privateKey));

        return this;
    }

    public SftpConfigurationBuilder CreateFileOnConnect(string remoteFilePath)
    {
        _fileCreationOptions = (true, remoteFilePath);
        return this;
    }
    public SftpConfiguration Build()
    {
        if (AuthenticationMethods.Count == 0)
        {
            throw new Exception("No AuthenticationMethods have been configured for Sftp Configuration");
        }

        var sftpConfiguration = new SftpConfiguration(_host, _port, _userName, AuthenticationMethods);
        if (_fileCreationOptions.createFile)
        {
            sftpConfiguration.CreateRemoteFile(_fileCreationOptions.remotePath);
        }

        return sftpConfiguration;
    }
}
public class SftpConfiguration
{
    internal string Host { get; }
    internal string UserName { get; set; }
    internal int Port { get; } = 22;
    internal List<AuthenticationMethod> AuthenticationMethods { get; }
    internal (bool createFile, string remoteFilePath) FileCreationOptions = (false, string.Empty);

    internal SftpConfiguration(string host, int port, string userName, List<AuthenticationMethod> authenticationMethods)
    {
        Host = host;
        Port = port;
        UserName = userName;
        AuthenticationMethods = authenticationMethods;
    }
    internal void CreateRemoteFile(string remoteFilePath)
    {
        FileCreationOptions = (true, remoteFilePath);
    }
}
