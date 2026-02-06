using System.Collections.Generic;

/// <summary>
/// 값을 전달하기 위한 데이터 클래스
/// </summary>
public class Parameter
{
    private Dictionary<string, object> _params = new Dictionary<string, object>();

    public object this[string key]
    {
        set
        {
            _params.Remove(key);
            _params.Add(key, value);
        }
        get
        {
            object returnValue = null;
            _params.TryGetValue(key, out returnValue);
            return returnValue;
        }
    }

    public Dictionary<string, object> GetParams
    {
        get { return _params; }
    }

    public Parameter() { }

    public Parameter(string key, object userdate)
    {
        this[key] = userdate;
    }

    public Parameter(Parameter copy)
    {
        foreach(KeyValuePair<string, object> data in copy._params)
        {
            _params.Add(data.Key, data.Value);
        }
    }

    public bool ContainsKey(string keyName)
    {
        if(null != _params && 0 < _params.Count)
            return _params.ContainsKey(keyName);
        else
            return false;
    }

    public void Remove(string keyName)
    {
        if(null != _params && true == _params.ContainsKey(keyName))
            _params.Remove(keyName);
    }

    public void Clear()
    {
        if (null != _params)
            _params.Clear();

        _params = null;
    }
}