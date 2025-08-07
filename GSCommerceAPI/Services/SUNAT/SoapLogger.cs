using System;
using System.IO;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Text.RegularExpressions;

namespace GSCommerceAPI.Services.SUNAT
{
    /// <summary>
    /// Logs SOAP request and response messages, masking sensitive fields like the password (clave).
    /// </summary>
    public class SoapLogger : IClientMessageInspector, IEndpointBehavior
    {
        /// <summary>
        /// Destination file for the logs.
        /// </summary>
        public string LogFilePath { get; set; } = "soap_log.txt";

        public object BeforeSendRequest(ref Message request, IClientChannel channel)
        {
            // Copy the message so it can be logged without consuming it
            var buffer = request.CreateBufferedCopy(int.MaxValue);
            var copy = buffer.CreateMessage();
            request = buffer.CreateMessage();

            var requestXml = copy.ToString();

            // Mask sensitive data
            requestXml = Regex.Replace(requestXml, @"(<clave>)(.*?)(</clave>)", "$1****$3", RegexOptions.IgnoreCase);
            requestXml = Regex.Replace(requestXml, @"(<password>)(.*?)(</password>)", "$1****$3", RegexOptions.IgnoreCase);
            requestXml = Regex.Replace(requestXml, @"(<wsse:Password.*?>)(.*?)(</wsse:Password>)", "$1****$3", RegexOptions.IgnoreCase);

            File.AppendAllText(LogFilePath, $"----- SOAP REQUEST -----{Environment.NewLine}{requestXml}{Environment.NewLine}");
            return null;
        }

        public void AfterReceiveReply(ref Message reply, object correlationState)
        {
            // Copy the message so it can be logged without consuming it
            var buffer = reply.CreateBufferedCopy(int.MaxValue);
            var copy = buffer.CreateMessage();
            reply = buffer.CreateMessage();

            var responseXml = copy.ToString();
            File.AppendAllText(LogFilePath, $"----- SOAP RESPONSE -----{Environment.NewLine}{responseXml}{Environment.NewLine}");
        }

        #region IEndpointBehavior Implementation
        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters) { }

        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
            clientRuntime.ClientMessageInspectors.Add(this);
        }

        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher) { }

        public void Validate(ServiceEndpoint endpoint) { }
        #endregion
    }
}
