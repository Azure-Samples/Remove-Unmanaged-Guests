# Remove Unmanaged Azure AD Guests 

A command line multitenant app that allows admins to identify and reset the redemption status of guests who have redeemed their B2B invitations with unmanaged (aka viral) Azure AD accounts. This app can run in report-only mode and/or automatically reset the redemption status of these accounts. If Email OTP is enabled, these guests will not be able to redeem with the same unmanaged Azure AD accounts.


## Getting Started

### Prerequisites

- To run the application and reset the redemption status of guests, you'll need an account with one of the following roles: Guest Inviter, User Administrator, Application Administrator and Directory Writer, or a Global Administator.
- If User Consent is restricted, you will need a Cloud Application Admin, Application Admin, or Global Admin to grant consent.
- Enable [Email OTP](https://docs.microsoft.com/en-us/azure/active-directory/external-identities/one-time-passcode#enable-email-one-time-passcode) if you wish to reset the redemption status of unmanaged Azure AD accounts and force them to redeem with a different method. If Email OTP is disabled, users will redeem with the same unmanaged accounts.
- [Visual Studio 2022](https://visualstudio.microsoft.com/downloads/) - This app requires .NET 6 


### Installation and Run Steps
1. Download the zipped code files.

![image](https://user-images.githubusercontent.com/49490355/153286919-df57da72-d027-4079-aaa4-da4271a5ab2c.png)

2. Extract the files to a file location of your choice.
3. Open Visual Studio 2022 and select **Open a project or solution**. Navigate to *.\Remove-Unmanaged-Guests-main\Remove-Unmanaged-Guests-main\source* and open **RemoveUnmanagedGuests.sln**.
4. The app by default points to a multitenant app hosted by Microsoft. You can run the application as is and it will create an enterprise app in your directory. However, if you wish to use your own app instance you may do so by performing the following:
    1. Create an [App Registration](https://docs.microsoft.com/en-us/azure/active-directory/develop/howto-create-service-principal-portal#register-an-application-with-azure-ad-and-create-a-service-principal). 
    2. Give the app a name. Under **Supported Account Types** select **Accounts in any organizational directory (Any Azure AD directory - Multitenant)**. Click **Register**. 
**Known limitation:** You cannot run this application as a single tenant application.
    3. Go to the **Authentication** blade of your app registration and set **Allow public client flows** to **Yes**. Click **Save**.
    4. Leave the API permissions as the default values - Microsoft Graph Delegated User.read.
    5. In Visual Studio, go to **appsettings.json**. Change the "clientId" value your application (client) Id and save the project.
5. Select **Build** and click **Build Solution**. 

![image](https://user-images.githubusercontent.com/49490355/154562959-6b50a9e2-2c04-4070-ba71-dddb934bfe38.png)

You should get a message saying "Build: 1 succeeded".

![image](https://user-images.githubusercontent.com/49490355/154563221-93432bb6-adae-4559-8ab8-8e6d92c46a6f.png)


5. Select **Debug** and click **Start Debugging** or press F5. 

![image](https://user-images.githubusercontent.com/49490355/154565892-2a23e7ed-ab28-4b7a-8758-014cde15ccfb.png)

6. A command line prompt should open. Enter your Azure AD [Tenant ID](https://docs.microsoft.com/en-us/azure/active-directory/fundamentals/active-directory-how-to-find-tenant) and press enter.

![image](https://user-images.githubusercontent.com/49490355/154566299-874b704c-0e06-4330-b59d-e8fe9f932361.png)

7. Select one of the following options (1, 2, 3, or other) and press enter:
- 1 - Reporting only = The app will identify how many viral users exist in your tenant and export a CSV file to the RemoveUnmanagedGuests file path.
- 2 - Reset the redemption status and send invitation email = The app will [reset the redemption status](https://docs.microsoft.com/en-us/azure/active-directory/external-identities/reset-redemption-status#use-the-azure-portal-to-reset-redemption-status) of all unmanaged Azure AD accounts and send the default invitation email.
- 3 - Reset the redemption status but do NOT send invitation email = The will reset the redemption status of all unmanaged Azure AD account but will NOT send an email.
- Any other key will exit the application.

![image](https://user-images.githubusercontent.com/49490355/153450212-86fb1393-2a04-4d3c-8eba-d9e3ce46ee01.png)

7. Once you have made your selection, open a browser, navigate to https://microsoft.com/devicelogin, and enter the device code given in the cmd prompt.

Note: This is using the [Device Authorization Grant](https://docs.microsoft.com/en-us/azure/active-directory/develop/v2-oauth2-device-code) Flow. Once you have the code, you have 15 minutes before it expires.

![image](https://user-images.githubusercontent.com/49490355/153293375-f3d80c38-b943-4679-b906-152ea93f782d.png)

8. Sign-in with an admin account who has appropriate permissions (see prereq list) and consent to the application. If you have previously consented, you will not see this prompt.

![image](https://user-images.githubusercontent.com/49490355/154717310-4794f9c1-e719-4d73-98ef-454a704b8cb1.png)

9. Select **Continue** to sign-in to the application.

![image](https://user-images.githubusercontent.com/49490355/153295803-fed3d15b-fe1d-478b-b07f-4fa43e41f01d.png)

10. Return to the cmd prompt. The application will begin searching through all guest users and identifying unmanaged (viral) accounts. Additionally, the application will reset these unamanged accounts' redemption status if you selected an option to do so.
11. Once the application is done running, the number of guests and viral users identified will be displayed and you will be returned to the menu. You can either select another option or click any other key to exit. If you want to see the list of viral users identified or reset, you can view the **UnmanagedUsers.csv** file in the *.\Remove-Unmanaged-Guests-main\Remove-Unmanaged-Guests-main\source\RemoveUnmanagedGuests\bin\Debug\net6.0* file path. 

Once you have enabled Email OTP and run this script, users will be unable to redeem invitations with unmanaged Azure AD accounts. You may safely delete this application from your Azure AD tenant.

