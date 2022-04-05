using kiop.Data;
using System.Diagnostics;

namespace kiop.Services
{
    public class ProvisionerService
    {
        public async Task ExecuteAsync(ProvisionerData kubeProperties, 
            Action<object, DataReceivedEventArgs> ansibleOutputHandler,
            Action<ScriptOutputLine> sshOutputHandler)
        {
            if (kubeProperties == null ||
                string.IsNullOrWhiteSpace(kubeProperties.HostAddress) ||
                string.IsNullOrWhiteSpace(kubeProperties.HostUsername) ||
                string.IsNullOrWhiteSpace(kubeProperties.HostPassword) ||
                string.IsNullOrWhiteSpace(kubeProperties.HostTargetNode) ||
                string.IsNullOrWhiteSpace(kubeProperties.HostTargetStorage) ||
                string.IsNullOrWhiteSpace(kubeProperties.AdminUser) ||
                string.IsNullOrWhiteSpace(kubeProperties.AdminPassword) ||
                string.IsNullOrWhiteSpace(kubeProperties.ClusterApiVip) ||
                string.IsNullOrWhiteSpace(kubeProperties.MasterIPs) ||
                string.IsNullOrWhiteSpace(kubeProperties.WorkerIPs) ||
                kubeProperties.TemplateId < 1 ||
                kubeProperties.MasterDiskSize < 3 ||
                kubeProperties.WorkerDiskSize < 3)
            {
                sshOutputHandler(new ScriptOutputLine("Wrong or incomplete information.", true));
                return;
            }

            sshOutputHandler(new ScriptOutputLine("Starting the provisioner..", true));
            
            string[] masterNodes = kubeProperties.MasterIPs.Split(',');
            string[] workerNodes = kubeProperties.WorkerIPs.Split(',');

            using (SshService sshService = new(kubeProperties.HostAddress, kubeProperties.HostUsername, kubeProperties.HostPassword, sshOutputHandler))
            {
                await sshService.ExecuteAsync($"wget {kubeProperties.ImageUrl}");
                
                await sshService.ExecuteAsync($"qm create {kubeProperties.TemplateId} --memory 4096 --cores 4  --name ubuntu-cloud-{kubeProperties.TemplateId} --net0 virtio,bridge=vmbr0");

                string imageFileName = kubeProperties.ImageUrl.Split('/').Last();

                await sshService.ExecuteAsync($"qm importdisk {kubeProperties.TemplateId} {imageFileName} {kubeProperties.HostTargetStorage}");
                await sshService.ExecuteAsync($"qm set {kubeProperties.TemplateId} --scsihw virtio-scsi-pci --scsi0 {kubeProperties.HostTargetStorage}:vm-{kubeProperties.TemplateId}-disk-0");
                await sshService.ExecuteAsync($"qm set {kubeProperties.TemplateId} --ide2 {kubeProperties.HostTargetStorage}:cloudinit");
                await sshService.ExecuteAsync($"qm set {kubeProperties.TemplateId} --boot c --bootdisk scsi0");
                await sshService.ExecuteAsync($"qm set {kubeProperties.TemplateId} --serial0 socket --vga serial0");
                await sshService.ExecuteAsync($"qm set {kubeProperties.TemplateId} --ipconfig0 ip=dhcp");
                
                sshService.CopySshPubKeyToHost();

                await sshService.ExecuteAsync($"qm set {kubeProperties.TemplateId} --sshkey /root/my_id_rsa.pub", noOutput: true);
                await sshService.ExecuteAsync($"qm set {kubeProperties.TemplateId} --ciuser {kubeProperties.AdminUser}");
                await sshService.ExecuteAsync($"qm set {kubeProperties.TemplateId} --cipassword {kubeProperties.AdminPassword}");

                await sshService.ExecuteAsync($"qm template {kubeProperties.TemplateId}");

                for (int i = 0; i < masterNodes.Length; i++)
                {
                    int id = kubeProperties.TemplateId + 1 + i;

                    await sshService.ExecuteAsync($"qm clone {kubeProperties.TemplateId} {id} --name k3s-master-{id} --full --storage {kubeProperties.HostTargetStorage} --target {kubeProperties.HostTargetNode}");
                    await sshService.ExecuteAsync($"qm set {id} --ipconfig0 ip={masterNodes[i]}/24,gw={kubeProperties.GatewayIp}");
                    await sshService.ExecuteAsync($"qm resize {id} scsi0 {kubeProperties.MasterDiskSize}G");
                    await sshService.ExecuteAsync($"qm start {id}");
                }

                for (int i = 0; i < workerNodes.Length; i++)
                {
                    int id = kubeProperties.TemplateId + 1 + masterNodes.Length + i;

                    await sshService.ExecuteAsync($"qm clone {kubeProperties.TemplateId} {id} --name k3s-worker-{id} --full --storage {kubeProperties.HostTargetStorage} --target {kubeProperties.HostTargetNode}");
                    await sshService.ExecuteAsync($"qm set {id} --ipconfig0 ip={workerNodes[i]}/24,gw={kubeProperties.GatewayIp}");
                    await sshService.ExecuteAsync($"qm resize {id} scsi0 {kubeProperties.WorkerDiskSize}G");
                    await sshService.ExecuteAsync($"qm start {id}");
                }

            }

            sshOutputHandler(new ScriptOutputLine("Preparing ansible hosts.ini file..", false));
            string hostsFile = await File.ReadAllTextAsync("/app/ansible/inventory/sample/hosts.ini");
            hostsFile = string.Format(hostsFile, string.Join("\n", masterNodes), string.Join("\n", workerNodes));
            await File.WriteAllTextAsync("/app/ansible/inventory/my-cluster/hosts.ini", hostsFile);

            sshOutputHandler(new ScriptOutputLine("Preparing ansible group_vars/all.yml file..", false));
            string varsFile = await File.ReadAllTextAsync("/app/ansible/inventory/sample/group_vars/all.yml");
            varsFile = string.Format(varsFile,
                kubeProperties.AdminUser,
                kubeProperties.ClusterApiVip,
                kubeProperties.AdminPassword,
                kubeProperties.LoadBalancerVipsRange);
            await File.WriteAllTextAsync("/app/ansible/inventory/my-cluster/group_vars/all.yml", varsFile);

            // wait 30 seconds for the vms to start and boot
            sshOutputHandler(new ScriptOutputLine("Waiting 30 seconds for the VMs to start and boot..", false));
            await Task.Delay(30 * 1000);
            AnsibleService ansibleService = new AnsibleService();
            await ansibleService.ExecuteAsync("ansible-playbook site.yml -i inventory/my-cluster/hosts.ini", ansibleOutputHandler);
        }
    }
}
