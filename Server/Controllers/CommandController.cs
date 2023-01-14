using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using Command.Base;
using Command.Exceptions;
using Command.Game;
using Command.Server.Helpers;
using Command.Server.Types;

namespace Command.Server.Controllers;

[ApiController("/command")]
public class CommandController : WebApiController
{

  [Route(HttpVerbs.Post, "/")]
  public void ExecuteCommand([JsonData] SerializableTextCommand command)
  {
    if (!Injections.ClientState.IsLoggedIn)
      throw new PlayerNotLoggedInException();

    if (command.Command is null or "" or "/")
      throw HttpException.BadRequest("必须指定要运行的命令");
    // only allow use of commands here for safety purposes (validating chat is hard)
    if (!command.Command.StartsWith("/") && command.SafeMode)
      throw HttpException.BadRequest("命令必须以斜扛开头");

    GameUtils.ResetAFKTimer();

    Injections.Framework.RunOnFrameworkThread(delegate
    {
      GameUtils.SendSanitizedChatMessage(command.Command, command.SafeMode);
    });
  }
}


