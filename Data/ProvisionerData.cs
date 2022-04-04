using System.ComponentModel.DataAnnotations;

namespace kiop.Data
{
    public class ProvisionerData
    {
        [Required, DataType(DataType.Url)]
        public string? ImageUrl { get; set; }

        [Required]
        [RegularExpression("^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$", ErrorMessage = "Must be in IP format xxx.xxx.xxx.xxx")]
        public string? HostAddress { get; set; }
        [Required]
        public string? HostUsername { get; set; }
        [Required]
        public string? HostPassword { get; set; }
        [Required]
        public string? HostTargetNode { get; set; }
        [Required]
        public string? HostTargetStorage { get; set; }

        [Required]
        public string? AdminUser { get; set; }
        [Required]
        public string? AdminPassword { get; set; }

        public string? TemplateId { get; set; }

        [Required]
        [RegularExpression("^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$", ErrorMessage = "Must be in IP format xxx.xxx.xxx.xxx")]
        public string? GatewayIp { get; set; }

        [Required]
        [RegularExpression("^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$", ErrorMessage = "Must be in IP format xxx.xxx.xxx.xxx")]
        public string? ClusterApiVip { get; set; }
        
        [Required]
        public string? MasterIPs { get; set; }
        
        [Required]
        public string? WorkerIPs { get; set; }

        [Required]
        public int MasterDiskSize { get; set; }
        [Required]
        public int WorkerDiskSize { get; set; }

        [Required]
        public string? LoadBalancerVipsRange { get; set; }
    }
}
