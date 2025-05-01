using NLog;
using Newtonsoft.Json.Linq;
using System;
using System.Management.Automation;

namespace Snowflake.Powershell
{
    [Cmdlet
        (VerbsCommon.New,
        "SFFolder",
        SupportsPaging = false,
        SupportsShouldProcess = false)]
    [OutputType(typeof(String))]
    public class NewFolderCommand : PSCmdlet
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private static Logger loggerConsole = LogManager.GetLogger("Snowflake.Powershell.Console");

        [Parameter(
            Mandatory = true,
            Position = 0,
            ValueFromPipeline = true,
            ValueFromPipelineByPropertyName = true,
            HelpMessage = "Application user context from authentication process")]
        public AppUserContext AuthContext { get; set; }

        [Parameter(
            Mandatory = true,
            Position = 1,
            ValueFromPipeline = true,
            ValueFromPipelineByPropertyName = true,
            HelpMessage = "Name of the folder to create")]
        public string FolderName { get; set; }

        protected override void ProcessRecord()

        {
            try
            {
                logger.Trace("BEGIN {0}", this.GetType().Name);

                // Call the Snowflake API to create the folder
                string createFolderApiResult = SnowflakeDriver.CreateFolder(this.AuthContext, FolderName, "CUSTOMER_ROLE");
                WriteVerbose($"[INFO] Raw API Response: {createFolderApiResult}");

                if (string.IsNullOrEmpty(createFolderApiResult))
                {
                    throw new ItemNotFoundException("Invalid response from creating folder");
                }

                JObject createFolderResponse = JObject.Parse(createFolderApiResult);
                string newFolderId = JSONHelper.getStringValueFromJToken(createFolderResponse, "createdFolderId");

                logger.Info("Created new folder '{0}' with ID '{1}'", FolderName, newFolderId);
                loggerConsole.Info("Created new folder '{0}' with ID '{1}'", FolderName, newFolderId);

                WriteObject(newFolderId);
            }
            catch (Exception ex)
            {
                logger.Error("{0} threw {1} ({2})", this.GetType().Name, ex.Message, ex.Source);
                logger.Error(ex);

                if (ex is ItemNotFoundException)
                {
                    this.ThrowTerminatingError(new ErrorRecord(ex, "FolderNotFound", ErrorCategory.ObjectNotFound, null));
                }
                else
                {
                    this.ThrowTerminatingError(new ErrorRecord(ex, "FolderCreationFailed", ErrorCategory.OperationStopped, null));
                }
            }
            finally
            {
                logger.Trace("END {0}", this.GetType().Name);
                LogManager.Flush();
            }
        }
    }
}