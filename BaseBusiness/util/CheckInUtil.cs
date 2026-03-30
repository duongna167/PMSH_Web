using System.Data;
using BaseBusiness.BO;
using BaseBusiness.Model;

namespace BaseBusiness.util
{
    public class CheckInUtil
    {
        public static void CheckInMasterFolio(string ConfirmationNo, ProcessTransactions pt)
        {
            DataTable _dt = pt.Select("SELECT ID, ProfileAgentID, AgentName, ProfileCompanyID, CompanyName FROM Reservation WITH (NOLOCK) WHERE ConfirmationNo = '" + ConfirmationNo + "' AND ReservationNo = 0 AND Status NOT IN (1,2,6) ");
            if (_dt.Rows.Count > 0)
            {
                for (int i = 0; i < _dt.Rows.Count; i++)
                {
                    int ProfileID = 0; string ProfileName = "";
                    if (int.Parse(_dt.Rows[i]["ProfileAgentID"].ToString() ?? string.Empty) > 0)
                    {
                        ProfileID = int.Parse(_dt.Rows[i]["ProfileAgentID"].ToString() ?? string.Empty);
                        ProfileName = _dt.Rows[i]["AgentName"].ToString() ?? string.Empty;
                    }
                    else
                    {
                        ProfileID = int.Parse(_dt.Rows[i]["ProfileCompanyID"].ToString() ?? string.Empty);
                        ProfileName = _dt.Rows[i]["CompanyName"].ToString() ?? string.Empty;
                    }
                    int RsvID = int.Parse(_dt.Rows[i]["ID"].ToString() ?? string.Empty);
                    DataTable _dtF = pt.Select("SELECT Count(ID) FROM Folio WITH (NOLOCK) WHERE ReservationID = " + RsvID + " AND IsMasterFolio =1 ");
                    if (int.Parse(_dtF.Rows[0][0].ToString() ?? string.Empty) >= 1)
                    {
                        pt.UpdateCommand("UPDATE Reservation with (rowlock) SET Status = 1 WHERE ID = " + RsvID + " ");

                        int ReservationOptionID = ReservationBO.GetReservationOptionID(RsvID, pt);
                        if (ReservationOptionID == 0)
                        {
                            ReservationOptionsModel mRO = new ReservationOptionsModel();
                            mRO.ReservationID = RsvID;
                            mRO.Billing = true;
                            pt.Insert(mRO);
                        }
                        else
                        {
                            ReservationOptionsModel mRO = (ReservationOptionsModel)pt.FindByPK("ReservationOptions", ReservationOptionID); ;
                            mRO.ID = ReservationOptionID;
                            mRO.Billing = true;
                            pt.Update(mRO);
                        }

                        //Transfer Deposit for mater
                        string err = "";
                        CasheringUtils.TranferDeposit(pt, ConfirmationNo, RsvID, ProfileID, ProfileName, ref err);
                    }
                    //CSS(17.01.2012)
                    //If is not exits
                    if (int.Parse(_dtF.Rows[0][0].ToString() ?? string.Empty) == 0)
                    {
                        //check Deposit
                        DataTable dtD = pt.Select("SELECT ID FROM dbo.DepositPayment WITH (NOLOCK) WHERE ReservationID = " + RsvID + " AND IsProcess = 0 AND TransactionCode = dbo.fnGetConfigsystem('DEPOSIT') ");
                        if (dtD.Rows.Count > 0)
                        {
                            //Create Folio
                            ReservationBO.CreateFolioNoRouting(RsvID, -1, pt);
                            //Update Rsv
                            pt.UpdateCommand("UPDATE Reservation with (rowlock) SET Status = 1 WHERE ID = " + RsvID + " ");

                            //Insert to RsvOption
                            int ReservationOptionID = ReservationBO.GetReservationOptionID(RsvID, pt);
                            if (ReservationOptionID == 0)
                            {
                                ReservationOptionsModel mRO = new ReservationOptionsModel();
                                mRO.ReservationID = RsvID;
                                mRO.Billing = true;
                                pt.Insert(mRO);
                            }
                            else
                            {
                                ReservationOptionsModel mRO = (ReservationOptionsModel)pt.FindByPK("ReservationOptions", ReservationOptionID); ;
                                mRO.ID = ReservationOptionID;
                                mRO.Billing = true;
                                pt.Update(mRO);
                            }

                            //Transfer Deposit for mater
                            string err = "";
                            CasheringUtils.TranferDeposit(pt, ConfirmationNo, RsvID, ProfileID, ProfileName, ref err);
                        }
                    }
                }
            }

            ////Kiểm tra xem master có không?
            //Expression exp = new Expression("ConfirmationNo", ConfirmationNo,"=");
            //exp = exp.And(new Expression("ReservationNo", "0", "="));
            //exp = exp.And(new Expression("Status", "1", "<>"));
            //exp = exp.And(new Expression("Status", "6", "<>"));
            //exp = exp.And(new Expression("Status", "2", "<>"));
            //ArrayList arr = pt.FindByExpression("Reservation", exp);
            ////Nếu có thì set trạng thái status = 1
            //if (arr.Count > 0)
            //{
            //    for (int i = 0; i < arr.Count; i++)
            //    {
            //        pt.UpdateCommand("UPDATE Reservation SET Status = 1 WHERE ID = " + ((ReservationModel)arr[i]).ID + " ");
            //    }
            //}
        }


    }
}