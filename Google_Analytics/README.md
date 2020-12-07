# C# script to access Users and Sessions data from Google Analytics

## Prerequisites
In order to access data through the Google Analytics API. first step is creating (or updating) an existing project in the Google Developer Console and enable the API.

## 1. Create a new project or use an existing one in Google Developer Console. <br>
Go to https://console.developers.google.com/project and sign in to your Google/Gmail account.
Once signed in, you'll see a list of existing projects (if there are any) or you can click Create project to create a new one.

<img src="../pics/ga1.png" width="600">

In the New Project dialog, enter a name and location for your project and then click Create.

## 2. Enable the Google Analytics API. <br>
Select the project, click the menu button top left corner, expand API & Services and click library. Scroll down and click Google Analytics API tile.

<img src="../pics/ga3.png" width="600">
<img src="../pics/ga4.png" width="300">
<img src="../pics/ga5.png" width="600">

## 3. Create service account credentials
In order to access Google Analytics data, you need to generate a Service account.
From the left navigation, click Credentials. Click Create credentials and select the Service account key option. <br>
Set the Service account dropdown to New service account. Enter the Name for your service account and then specify a Role. Ensure that the JSON key type is selected. <br>
Click Create to generate and download a certificate file. This file will be needed to connect from SSIS.

<img src="../pics/ga6.png" width="600">
<img src="../pics/ga7.png" width="400">

The Service account created dialog is displayed and shows the password for the private key. Copy and paste this password into Notepad for later use. <br>
Close the dialog to see the list of service account keys which includes the one you just created. <br>
Click Manage service accounts in the top-right corner.

<img src="../pics/ga8.png" width="700">

Copy and paste the Service account email address into Notepad for later use.

<img src="../pics/ga9.png" width="700">


## 4. Authorization
*NOTE: you probably won’t have the Admin privileges for Google Analytics, so you will need to reach a person who has them.* <br>
Now that the client credentials have been generated, sign in to your Google Analytics account in order to give authorization to these credentials. <br>
Go to the Admin area and click User Management.

<img src="../pics/ga10.png" width="400">

In the Add permissions box, paste the Service account email address that you recorded earlier. Then click Add.

<img src="../pics/ga11.png" width="600">

## 4. Code for requesting data fromm Google Analytics and insert into a database with SSIS
The implementation in SSIS to request data from GA is based on a Script Component in C#. The component works as a source, first executing the Authentication using the json file obtained in previous steps, and then sending the request for data.
In the example we request data for Users and Sessions in a specific Date Range.
