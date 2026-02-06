using Barebones.MasterServer;
using System;
using UnityEngine;

public static class DataManager
{
    public static UserDataManager UserDataManager { get; private set; }

    public static void Login(AccountInfoPacket accountInfoPacket)
    {
        UserDataManager = new UserDataManager(accountInfoPacket);
    }

    public static void Logout()
    {
        UserDataManager.Release();
        UserDataManager = null;
    }
}
