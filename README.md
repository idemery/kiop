# K3s installation on ProxMox (kiop)
Automates the installation of K3s Kubernetes cluster on ProxMox

# How to use
```docker run -p [port]:80 -v /path/to/your/.ssh:/root/.ssh:ro -d --name kiop idemery/kiop:alpha```

Example:
```docker run -p 8080:80 -v /home/idemery/.ssh:/root/.ssh:ro -d --name kiop idemery/kiop:alpha```

Open your browser on http://localhost:8080 and fill in the information then click submit. Wait and watch the console screen on bottom.

![kiop](https://github.com/idemery/kiop/blob/master/Screen%20Shot%202022-04-05%20at%201.52.03%20PM.png)

# How it works
By mapping your ssh key folder to this container (notice the read only :ro ), this application does the following in order: 

1. Connects to your ProxMox server using SSH session with IP, username and password
2. Downloads a linux cloud image (suggested is ubuntu focal minimal cloud image)
3. Uploads your public ssh key to the ProxMox server
4. Creates virtual machine template based on the cloud image and sets the ssh key cloud-init parameter to your ssh key
5. Creates master and worker nodes by cloning the template based on the count of master and worker IPs entered and sets the network configuration and disk size using cloud-init parameters
6. Copies and configures ansible/inventory/sample/hosts.ini and ansible/inventory/sample/group_vars/all.yml into my-cluster folder 
7. Executes ansible-playbook process

# Notes
- Used .NET Core 6:latest docker image because its based on debian which is easy to install ansible on (thanks to geerlingguy's repo mentioned below)
- Installed ssh package to be able to connect to the ProxMox server, issue commands, and upload user's public ssh key
- For ansible to be able to connect to the VMs I referenced the private ssh key of the user in /etc/ansible/ansible.cfg and used host_key_checking = False and private_key_file = /root/.ssh/id_rsa (see the Dockerfile)
- Used Blazor framework to automatically get status from server without worrying about the underlying web sockets and SignalR implementation

# Thank you
Thanks to all the following GitHub repositories
* [techno-tim/k3s-ansible](https://github.com/techno-tim/k3s-ansible)
* [geerlingguy/docker-debian10-ansible](https://github.com/geerlingguy/docker-debian10-ansible)
* [k3s-io/k3s-ansible](https://github.com/k3s-io/k3s-ansible)
* [geerlingguy/turing-pi-cluster](https://github.com/geerlingguy/turing-pi-cluster)
* [212850a/k3s-ansible](https://github.com/212850a/k3s-ansible) 




