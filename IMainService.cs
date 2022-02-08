using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

namespace TFTService
{
    [ServiceContract]
    public interface IMainService
    {
        [OperationContract]
        string ComputeNumber(string input);

        [OperationContract]
        Dictionary<string, string> GetNumbers();
    }
}
