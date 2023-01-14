using System;

namespace Command.Exceptions;


public class IllegalGameStateException : InvalidOperationException
{
  public IllegalGameStateException(string message) :
      base(message)
  { }
}

public class PlayerNotLoggedInException : IllegalGameStateException
{
  public PlayerNotLoggedInException() :
      base("玩家未登录游戏")
  { }
}