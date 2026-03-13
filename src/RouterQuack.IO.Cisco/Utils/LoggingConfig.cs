using System.Text;

namespace RouterQuack.IO.Cisco.Utils;

internal static class LoggingConfig
{
    internal static void ApplyLoggingConfig(StringBuilder builder)
    {
        builder.AppendLine(LogConfig);
        builder.AppendLine(ArchivingConfig);
    }

    private const string LogConfig =
        """
        ! ================= Logging =================
        logging on
        service timestamps log datetime msec
        service timestamps debug datetime msec
        logging userinfo
        logging buffered 64000
        logging console critical
        logging monitor debugging
        !
        !
        """;

    private const string ArchivingConfig =
        """
        ! ================= Archiving =================
        archive
         log config
          logging enable
          logging size 200
          hidekeys
        !
        !
        """;
}