using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel;

namespace DeploySAJob.Models
{
    public class SAJobConfigModel
    {
        // everything about the job
        [DisplayName("Your subscription ID to deploy the job to")]
        public String subscriptionID { get; set; } //= "8d3766f2-c136-4ffe-94b3-046a01ac3239";
        [DisplayName("Resource Group Name for the Job")]
        public String resourceGroupName { get; set; } = "mediahackfest";
        [DisplayName("StreamAnalytics Job Name")]
        public String streamAnalyticsJobName { get; set; } = "mediahacksa";
        [DisplayName("StreamAnalytics Job Input Name")]
        public String streamAnalyticsInputName { get; set; } = "mediahackin";
        [DisplayName("StreamAnalytics Job Location")]
        public String location { get; set; } = "eastus2";

        //everything about the input EventHub
        public String ServiceBusNamespace { get; set; } = "mediahackns";
        public String EventHubName { get; set; } = "mediahackeh";
        public String SharedAccessPolicyName { get; set; } = "mediahackpolicy";
        public String SharedAccessPolicyKey { get; set; } //= "t5T5Wnsyp4IyQRTkkHyczymPyQYRDuyVoXZOjXtAuu0=";
    }
}