This web application has a single purpose, to get an OAuth Bearer AccessToken to use in a server
application, like https://bitbucket.org/rgl/developerreactions.

When creating a new application on Meo Cloud / Dropbox use the following callback url:

	http://localhost:8008/callback

Then, open http://localhost:8008/ and insert the application ClientId and ClientSecret.

Then click the Authorize button. If all goes well, you should end up with a AccessToken, which
you can place inside MeoCloud.cs to try it out; also change Program.cs to call MeoCloud.cs; see
the Program.Main method.