# AstroLife

Front End: dotnet blazorwasm

Back End: dotnet web api


Front end packages:

   Top-level Package                                               Requested   Resolved
   > Blazored.SessionStorage                                       2.4.0       2.4.0
   > Microsoft.AspNetCore.Components.WebAssembly                   9.0.4       9.0.4
   > Microsoft.AspNetCore.Components.WebAssembly.DevServer         9.0.4       9.0.4
   > Microsoft.AspNetCore.SignalR.Client                           9.0.4       9.0.4
   > Microsoft.Extensions.Caching.Memory                           9.0.4       9.0.4
   > Microsoft.NET.ILLink.Tasks                              (A)   [9.0.4, )   9.0.4
   > Microsoft.NET.Sdk.WebAssembly.Pack                      (A)   [9.0.4, )   9.0.4
   > Newtonsoft.Json                                               13.0.3      13.0.3
   > System.Net.Http.Json                                          9.0.4       9.0.4

backend packages: 

   Top-level Package                                        Requested   Resolved
   > Microsoft.AspNetCore.Authentication.JwtBearer          9.0.4       9.0.4
   > Microsoft.AspNetCore.Cors                              2.3.0       2.3.0
   > Microsoft.AspNetCore.Identity.EntityFrameworkCore      9.0.4       9.0.4
   > Microsoft.AspNetCore.OpenApi                           9.0.4       9.0.4
   > Microsoft.AspNetCore.SignalR                           1.2.0       1.2.0
   > Microsoft.AspNetCore.SignalR.Core                      1.2.0       1.2.0
   > Microsoft.EntityFrameworkCore                          9.0.4       9.0.4
   > Microsoft.EntityFrameworkCore.InMemory                 9.0.4       9.0.4
   > Microsoft.EntityFrameworkCore.SqlServer                9.0.4       9.0.4
   > Microsoft.Extensions.Caching.Memory                    9.0.4       9.0.4
   > Microsoft.Extensions.Http                              9.0.4       9.0.4
   > SwissEphNet                                            2.8.0.2     2.8.0.2
   > System.Text.Json                                       9.0.4       9.0.4


add apikeys in appsettings.json


make sure to properly congfig new pages and their endpoints in navmenu.html

   
---------------------    
add to index.html    

    <script>
        function scrollToBottom() {
            let chatContainer = document.getElementById("chatContainer");
            if (chatContainer) {
                chatContainer.scrollTop = chatContainer.scrollHeight;
            }
        }
    </script>


