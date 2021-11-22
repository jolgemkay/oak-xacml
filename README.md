# oak-xacml

DISCLAIMER:
This should only be used for test and learning purposes. 
It is not intended for any production environment. 


Application for testing XACML policies and request against Authzforce server. 


Application setup: 

1. Setup Authzforce server and create a domain. See docs: https://authzforce-ce-fiware.readthedocs.io/en/latest/.  

2. Make sure you have .Net 5 runtime installed. See: https://dotnet.microsoft.com/download/dotnet/5.0. Make sure you have “dotnet” PATH-variable set.  

3. Copy “oak-xacml” folder to suitable location. Application must have network access to AuthzForce server. Try to ping it to be sure. (Should be straightforward if application and authzforce both are localhost) 

4. In appsettings.json fill in “authzfource_domain”: “YOUR DOMAIN” and “authzforce_URL”: “http://{HOST}:{PORT}/authzforce-ce/” 

5. From your shell of choice, navigate to “oak-xacml” folder and run “dotnet run oak-xacml".  

6. In your browser go to “localhost:5001”.  

 

Application usage:  

1. Upload the policy you want to test. When file is chosen, the application immediately tries to set it as the new root policy version in Authzforce. If the policy isn’t wrapped in in <PolicySet>, then the application does this with policy-combining-algorithm set to “permit-overrides”. If policy is accepted you get a green return message at the bottom, and a new entry in the policy history table. Version number is set by application. If the policy is not conformed, the policy is not set, and you get a yellow warning with error message. The policy history table will always show the active (current) root policy on top.  

2. Send request to PDP by uploading request file. If there is something wrong with the file, you get a yellow warning with error message. If the PDP processes the file, it inserts a new entry in the decision request history table. There you will also see the final decision made, and what policy it tested against. (root version).  
