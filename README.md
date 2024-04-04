# Network Toolkit

I have been writing a program designed to be an all-in-one-place network troubleshooting tool. Utilizing Avalonia UI and .NET 7 to create an interesting interface with a degree of speed and functionality. The greatest focus however is diversity in features.  

Please note that this tool is still very early in it's development.

## The Features

### IP Config View: 

• Takes every bit of information that is available about the local network interfaces of a computer and displays it in an easy to read and compact area.  
• Interface model and configuration details displaying useful information about the description and status of the interface.  
• Traffic statistics giving a breakdown on what type of traffic is traversing the adapter.  
• IP configuration including Gateway, DNS, DHCP, and other addresses related to your config.  
• A routing table showing your adapters decision making process for where to send packets.  
• An ARP table displaying the MAC addresses of noisy neighboring devices.  
• All live updating every five seconds.  

![image](https://github.com/joseph-mckee/NetworkToolkit/assets/134827079/6cbe81ed-a975-4898-9039-afde6fb1bd79)

### Network Scanner:

• Scans the network for a range of specified IP addresses.  
• Displays results in an organized and sortable data grid.  
• Includes MAC addresses, Vendor Information, and Hostnames of devices (only when available).  
• Fast and efficient scanning only utilizing the protocol that fits the situation.  
• Able to scan a wide range of addresses in a short amount of time.  

![image](https://github.com/joseph-mckee/NetworkToolkit/assets/134827079/29dfe8f2-f974-497c-b66a-2de1cd1b5478)

### Ping Tool:

• Exposes all of the options you could ever need for pinging something.  
• No limits.  
• Provides useful statistics about packet loss.

![image](https://github.com/joseph-mckee/NetworkToolkit/assets/134827079/060d82e1-5a24-470b-9ba1-2ab8debd1084)

### Traceroute Tool:

• Attempts to make the traceroute process more bearable by making DNS queries optional and giving the user more control over its functionality.

![image](https://github.com/joseph-mckee/NetworkToolkit/assets/134827079/1b4e9e70-8ab8-4aa1-8d6d-f3f84f28b4f8)

## Plans

In the future I will be implementing an SNMP module (in the works) that will allow for testing and information gathering purposes allowing users to perform walks primarily as well as other SNMP queries.

I will also be adding  a DNS tool for testing and viewing and a port scanner. I am debating on whether or not to include a speed test client (local or otherwise).
