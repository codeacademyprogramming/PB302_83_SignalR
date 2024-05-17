using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Pustok.Models;

namespace Pustok
{
    public class PustokHub:Hub
    {
        private readonly UserManager<AppUser> _userManager;

        public PustokHub(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }

        public override Task OnConnectedAsync()
        {

            if (Context.User.Identity.IsAuthenticated)
            {
                AppUser user = _userManager.GetUserAsync(Context.User).Result;
                user.ConnectionId = Context.ConnectionId;

                var result = _userManager.UpdateAsync(user).Result;

                Clients.All.SendAsync("ShowConnected", user.Id);
            }

            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            if (Context.User.Identity.IsAuthenticated)
            {
                AppUser user = _userManager.GetUserAsync(Context.User).Result;
                user.ConnectionId = null;
                user.LastConnectedAt = DateTime.Now;

                var result = _userManager.UpdateAsync(user).Result;
            }
            return base.OnDisconnectedAsync(exception);
        }
    }
}
