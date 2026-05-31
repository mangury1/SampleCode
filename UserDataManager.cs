using Barebones.MasterServer;
using System;

public class UserDataManager
{
    /// <summary>
    /// 로그인할시 시간과 각 데이터를 초기화
    /// </summary>
    /// <param name="accountInfoPacket"></param>
    public UserDataManager(AccountInfoPacket accountInfoPacket)
    {
        DateTime now = DateTime.Now;
        DateTime serverTime;
        
        //_clientTimer = new Timer(now);
        if(false == DateTime.TryParse(accountInfoPacket.LoginTime, out serverTime))
            serverTime = now;

        DateTime utcTime = serverTime.AddHours(UtcBaseGapHours);
        TimeSpan utcSpan = new TimeSpan();
        utcSpan = now - utcTime;
        UtcGapHours = (int)utcSpan.Hours;
        UtcGapMinutes = (int)utcSpan.Minutes;

        _clientTimer = new Timer(now);
        _serverTimer = new Timer(serverTime);
        _currentTimer = new Timer(utcTime.AddHours(UtcGapHours).AddMinutes(UtcGapMinutes));

        UserInfo = new UserInfoManager();
        UserInventory = new InventoryManager();
        UserQuest = new QuestManager();
    }

    /// <summary>
    /// LogOut시 초기화 함수
    /// </summary>
    public void Release()
    {
        UserInfo.Release();
        UserQuest.Release();
        UserInventory.Release();

        UserInfo = null;
        UserQuest = null;
        UserInventory = null;
    }

    // 한국시간이 온다 UTC로 맞춰야 해서 9시간 뺀다.
    public int UtcBaseGapHours { get { return -9; } }

    //시간차
    public int UtcGapHours { get; private set; }
    public int UtcGapMinutes { get; private set; }

    /// <summary>
    /// 로그인시 서버시간
    /// </summary>
    private Timer _serverTimer;
    public DateTime ServerTime { get { return _serverTimer.ServerTime; } }

    /// <summary>
    /// 현지 시간으로 계산된 시간 (기기에서 시간값을 가지고와 변조될수 있어 UI표기 용으로만 사용)
    /// </summary>
    private Timer _currentTimer;
    public DateTime CurrentTime { get { return _currentTimer.ServerTime; } }

    /// <summary>
    /// DateTime.Now보다 좋다고 한다. 이쁘지않다
    /// </summary>
    private Timer _clientTimer;
    public DateTime ClientTime { get { return _clientTimer.ServerTime; } }

    public UserInfoManager UserInfo { get; private set; }
    public QuestManager UserQuest { get; private set; }
    public InventoryManager UserInventory { get; private set; }

}