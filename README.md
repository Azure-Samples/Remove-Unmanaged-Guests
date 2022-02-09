# Remove Unmanaged Azure AD Guests 

A command line multitenant app that allows admins to identify and reset the redemption status of guests who have redeemed their B2B invitations with unmanaged (aka viral) Azure AD accounts. This app can run in report-only mode and/or automatically reset the redemption status of these accounts. If Email OTP is enabled, these guests will not be able to redeem with the same unmanaged Azure AD accounts.


## Getting Started

### Prerequisites

- An admin account with one of the following roles: User Administrator, Guest Inviter, Application Administrator and Directory Writer, or is a Global Administator
- [Enable Email OTP](https://docs.microsoft.com/en-us/azure/active-directory/external-identities/one-time-passcode#enable-email-one-time-passcode) if you wish to reset the redemption status of unmanaged Azure AD accounts and force them to redeem with a different method.
- Microsoft.NETCore.App Version 6.0.0 or greater

Note: To verify if you have Microsoft.NETCore.App Version 6.0.0 or greater installed run the following:
```
dotnet --list-runtimes
```

![image](https://user-images.githubusercontent.com/49490355/153287705-190bc4a3-c1ca-45ee-9da3-16ef57b90937.png)

If you do not have the minimum version, click [here](https://dotnet.microsoft.com/en-us/download/dotnet/6.0/runtime) to download it. Select the appropriate OS and select "Download x64" under the "Run console apps" section.

![image](https://user-images.githubusercontent.com/49490355/153292885-5516e6ea-2c8e-4f8c-b25d-adcd6d6eaad1.png)


### Installation and Run Steps
1. Download the zipped code files.

![image](https://user-images.githubusercontent.com/49490355/153286919-df57da72-d027-4079-aaa4-da4271a5ab2c.png)

2. Extract the files to a file location of your choice.
3. Open a cmd prompt and change directories to the RemoveUnmanagedGuests file. For example:
```
cd "C:\Users\JohnDoe\OneDrive - Microsoft\Documents\Remove-Unmanaged-Guests-main\Remove-Unmanaged-Guests-main\RemoveUnmanagedGuests"
```
4. Enter the following and press enter:
```
RemoveUnmanagedGuests.exe
```
5. When prompted, enter your Azure AD [Tenant ID](https://docs.microsoft.com/en-us/azure/active-directory/fundamentals/active-directory-how-to-find-tenant) and press enter.
6. Select one of the following options and press enter:
- 1 - Reporting only = The app will identify how many viral users exist in your tenant and export a CSV file to the RemoveUnmanagedGuests file path.
- 2 - Reset the redemption status and send invitation email = The app will [reset the redemption status](https://docs.microsoft.com/en-us/azure/active-directory/external-identities/reset-redemption-status#use-the-azure-portal-to-reset-redemption-status) of all unmanaged Azure AD accounts and send the default invitation email.
- 3 - Reset the redemption status but do NOT send invitation email = The will reset the redemption status of all unmanaged Azure AD account but will NOT send an email.

Insert Screenshot here

7. Once you have made your selection, open a browser, navigate to https://microsoft.com/devicelogin, and enter the device code given in the cmd prompt.

![image](https://user-images.githubusercontent.com/49490355/153293375-f3d80c38-b943-4679-b906-152ea93f782d.png)

8. Sign-in with an admin account who has appropriate permissions (see prereq list) and consent to the application.

![image](https://user-images.githubusercontent.com/49490355/153294601-bef4c95b-c562-4a42-a9f7-deee65bc262e.png)

9. Return to the cmd prompt. The application will begin searching through all guest users and identifying unmanaged (viral) accounts. Additionally, the application will reset these unamanged accounts' redemption status if you selected an option to do so.

Once you have enabled Email OTP and run this script, users will be unable to redeem invitations with unmanaged Azure AD accounts. You may safely delete this application from your Azure AD tenant.

