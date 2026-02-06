using System;
using System.Collections.Generic;

/// <summary>
/// 클라이언트가 사용하기 위한 인벤토리 아이템 정보 클래스
/// </summary>
public class ClientInvenItemInfo
{
    #region ----------------------------- 서버에서 주는 데이터 

    public ulong ItemID { get; protected set; }
    public int ItemCode { get; protected set; }
    public int Count { get; protected set; }
    public int Level { get; protected set; }
    #endregion

    #region ----------------------------- 테이블 데이터 

    protected ItemsModel _itemsModel = null;

    public ItemsModel GetItemsModel { get { return _itemsModel; } }
    public string ItemName { get { return null != _itemsModel ? _itemsModel.Name : ""; } }
    public ITEM_CATEGORY Category { get { return null != _itemsModel ? _itemsModel.Category : ITEM_CATEGORY.NONE; } }
    public ITEM_TYPE ItemType { get { return null != _itemsModel ? _itemsModel.Type : ITEM_TYPE.NONE; } }
    public ITEM_GRADE BaseGradeType { get { return null != _itemsModel ? _itemsModel.Grade : ITEM_GRADE.NONE; } }
    public short BaseGrade { get { return null != _itemsModel ? _itemsModel.Rank : (short)0; } }

    #endregion

    #region ----------------------------- 체크 변수

    /// <summary>
    /// 판매 가능 상태인지
    /// </summary>
    public bool CheckSell { get { return true; } }

    /// <summary>
    /// 나누기 가능 아이템 인지
    /// </summary>
    public bool CheckDivide { get { return true; } }

    public bool NewItem { get; protected set; }

    #endregion

    #region ----------------------------- 클라이언트 변수

    #endregion

    public ClientInvenItemInfo() { }

    public ClientInvenItemInfo(InventoryData inventoryData, bool newItem = false)
    {
        UpdateData(inventoryData, newItem);
    }

    public ClientInvenItemInfo(ClientInvenItemInfo inventoryData)
    {
        UpdateData(inventoryData);
    }

    #region ----------------------------- 데이터 갱신

    public virtual void UpdateData(InventoryData inventoryData, bool newItem = false)
    {//UpdateData
        if(null == inventoryData)
            Debug.LogError("__ !! Err ClientInvenItemInfo UpdateData InventoryData Null !!");
        else
        {
            ItemID = inventoryData.Id;
            ItemCode = inventoryData.GameItemCode;
            Count = inventoryData.Count;
            Level = inventoryData.Level;

            NewItem = newItem;

            if(false == ItemsDataManager.Instance.FindItemBy(ItemCode, out _itemsModel))
                Debug.LogError("__ !! Err ClientInvenItemInfo UpdateData 잘못된 아이템코드다 !! ItemCode = " + ItemCode);
        }
    }//UpdateData

    public virtual void UpdateData(ClientInvenItemInfo inventoryData)
    {//UpdateData
        if(null == inventoryData)
            Debug.LogError("__ !! Err ClientInvenItemInfo UpdateData InventoryData Null !!");
        else
        {
            ItemID = inventoryData.ItemID;
            ItemCode = inventoryData.ItemCode;
            Count = inventoryData.Count;
            Level = inventoryData.Level;
            NewItem = inventoryData.NewItem;

            if(false == ItemsDataManager.Instance.FindItemBy(ItemCode, out _itemsModel))
                Debug.LogError("__ !! Err ClientInvenItemInfo UpdateData 잘못된 아이템코드다 !! ItemCode = " + ItemCode);

            SetGradeModel();
        }
    }//UpdateData


    public void SetItemCount(int setCount)
    {
        Count = setCount;
    }
}


