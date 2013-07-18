using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Net.Security;

namespace WordSampleWebRole
{
    [ServiceContract]
    public interface IWSTrustFeb2005Contract
    {
        [OperationContract(ProtectionLevel = ProtectionLevel.EncryptAndSign,
            Action = "http://schemas.xmlsoap.org/ws/2005/02/trust/RST/Issue",
            ReplyAction = "http://schemas.xmlsoap.org/ws/2005/02/trust/RSTR/Issue",
            AsyncPattern = true)]
        IAsyncResult BeginIssue(Message request, AsyncCallback callback, object state);
        Message EndIssue(IAsyncResult asyncResult);
    }
}
