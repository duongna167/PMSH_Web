-- Stored procedures cho chuc nang 'Export Profile to Excel' (WinForm frmProfileExport_V2)
-- Tuong thich SQL Server 2014.

-- Nguon: oneSPMSh_HDDT
IF OBJECT_ID('dbo.spGuestProfileSearch', 'P') IS NOT NULL DROP PROCEDURE dbo.spGuestProfileSearch;
GO
-- spGuestProfileSearch '2023-03-21','2023-03-21'
create  PROCEDURE [dbo].[spGuestProfileSearch]
@fromdate Datetime,
@todate Datetime

AS
BEGIN	
	SET NOCOUNT ON;
	WITH tbRsv AS
	(
		SELECT  ROW_NUMBER() over(partition by p.Firstname order by a.CheckOutDate desc) as _Row, 
			a.Title as TitleName, p.Firstname + ' ' + p.MiddleName as Firstname, p.LastName, 
				p.Email, p.Telephone + ' ' + p.HandPhone  as Phone, p.City, p.PassPort as Passpost, a.State, dbo.Country.Name as Country, dbo.Nationality.Code as Nationality, a.ArrivalDate as CheckInDate,a.DepartureDate as CheckOutDate, a.RoomNo ,'' as nps
				,p.PassPort as ID, a.Language, a.ConfirmationNo, a.OriginCode, s.Description as SourceCode, a.MarketCode, p.FullAccount, FD.RoomID, FD.RoomType,
				a.NoOfNight, a.NoOfAdult as NoOfAdults, a.RateCode, a.AgentName, p.VIPReason, a.Rate, a.MemberNo CRSNo,

				(select sum(FD.AmountBeforeTax) from FolioDetail FD inner join Reservation R on FD.ReservationID=R.ID
				inner join Profile Pf on R.ProfileIndividualID=Pf.ID where FD.GroupCode='Room' and FD.FolioID=F.ID and FD.CurrencyID='VND' and RowState = PostType) as RoomRevenueLocal, 
				
				(select sum(FD.AmountBeforeTax) from FolioDetail FD inner join Reservation R on FD.ReservationID=R.ID
				inner join Profile Pf on R.ProfileIndividualID=Pf.ID where FD.GroupCode='F&B' and FD.FolioID=F.ID and FD.CurrencyID='VND' and RowState =  1 and IsSplit=1) as FBRevenueLocal, 
				
				isnull((select sum(FD.AmountBeforeTax) from FolioDetail FD inner join Reservation R on FD.ReservationID=R.ID
				inner join Profile Pf on R.ProfileIndividualID=Pf.ID where FD.GroupCode='Room' and FD.FolioID=F.ID and FD.CurrencyID='VND' and RowState = PostType) ,0) + 
				isnull((select sum(FD.AmountBeforeTax) from FolioDetail FD inner join Reservation R on FD.ReservationID=R.ID
				inner join Profile Pf on R.ProfileIndividualID=Pf.ID where FD.GroupCode='F&B' and FD.FolioID=F.ID and FD.CurrencyID='VND' and RowState =  1 and IsSplit=1),0)
				as TotalRevenueLocal,

				(select sum(FD.AmountBeforeTax) from FolioDetail FD inner join Reservation R on FD.ReservationID=R.ID
				inner join Profile Pf on R.ProfileIndividualID=Pf.ID where FD.GroupCode='Room' and FD.FolioID=F.ID and FD.CurrencyID='USD'  and RowState = PostType) as RoomRevenueUSD,
				
				(select sum(FD.AmountBeforeTax) from FolioDetail FD inner join Reservation R on FD.ReservationID=R.ID
				inner join Profile Pf on R.ProfileIndividualID=Pf.ID where FD.GroupCode='F&B' and FD.FolioID=F.ID and FD.CurrencyID='USD' and RowState =  1 and IsSplit=1) as FBRevenueUSD, 
				
				isnull((select sum(FD.AmountBeforeTax) from FolioDetail FD inner join Reservation R on FD.ReservationID=R.ID
				inner join Profile Pf on R.ProfileIndividualID=Pf.ID where FD.GroupCode='Room' and FD.FolioID=F.ID and FD.CurrencyID='USD'  and RowState = PostType),0) +
				isnull((select sum(FD.AmountBeforeTax) from FolioDetail FD inner join Reservation R on FD.ReservationID=R.ID
				inner join Profile Pf on R.ProfileIndividualID=Pf.ID where FD.GroupCode='F&B' and FD.FolioID=F.ID and FD.CurrencyID='USD' and RowState =  1 and IsSplit=1),0)
				 as TotalRevenueUSD,
				
				--(select sum(FD.AmountBeforeTax) from FolioDetail FD inner join Reservation R on FD.ReservationID=R.ID
				--inner join Profile Pf on R.ProfileIndividualID=Pf.ID where FD.GroupCode='MIS' and FD.FolioID=F.ID and FD.CurrencyID='VND' and RowState = PostType) as MisRevenueLocal, 
			

				FD.CurrencyMaster, 
				(select top 1 E.ExChangeRate from ExchangeRate E, Reservation R 
				where DATEDIFF(day,E.DateTime,R.CheckOutDate)=0 and E.FromCurrencyID='USD' order by E.DateTime desc ) as ExChangeRate

		FROM    dbo.Profile p INNER JOIN
				--dbo.Title t on t.id = p.TitleID INNER JOIN
				dbo.Reservation a WITH (NOLOCK) ON p.ID = a.ProfileIndividualID 
				INNER JOIN dbo.Folio F WITH (NOLOCK) ON F.ReservationID= a.ID
				INNER JOIN dbo.FolioDetail FD WITH (NOLOCK) ON FD.FolioID=F.ID
				LEFT JOIN dbo.Country ON p.CountryID = dbo.Country.ID 
				LEFT JOIN dbo.Nationality ON p.NationalityID = dbo.Nationality.ID
				INNER JOIN dbo.Source s WITH(NOLOCK) ON a.SourceID = s.ID

		WHERE   ((datediff(day,a.CheckOutDate,@fromdate  )<=0 and  datediff(day,a.CheckOutDate,@todate )>=0 and a.Status IN (2)) or 
				(datediff(day,a.CheckInDate,@fromdate  )<=0 and  datediff(day,a.CheckInDate,@todate )>=0 and a.Status IN (1)))-- ngọc thêm
		And RoomNo >0 and a.ReservationNo>0 AND a.RoomType <> 'XXX' 

		 
	)
	SELECT	a.*
	FROM	tbRsv a 
	where a._row = 1
END
GO

-- Nguon: oneSPMS_AVANI_HD (oneSPMSh_HDDT khong co SP nay)
IF OBJECT_ID('dbo.spGuestProfileSearchGuest', 'P') IS NOT NULL DROP PROCEDURE dbo.spGuestProfileSearchGuest;
GO
-- spGuestProfileSearchGuest '01/01/2014','03/31/2019'
CREATE PROCEDURE [dbo].[spGuestProfileSearchGuest]
@fromdate Datetime,
@todate Datetime

AS
BEGIN	
	SET NOCOUNT ON;
	WITH tbRsv AS
	(
		SELECT  ROW_NUMBER() over(partition by p.Firstname order by a.CheckOutDate desc) as _Row, 
			a.Title as TitleName, p.Firstname + ' ' + p.MiddleName as Firstname, p.LastName, 
				p.Email, p.Telephone + ' ' + p.HandPhone  as Phone, p.City, p.PassPort as Passpost, a.State, dbo.Country.Name as Country, dbo.Nationality.Code as Nationality, a.ArrivalDate as CheckInDate,a.DepartureDate as CheckOutDate, a.RoomNo ,'' as nps
				,p.PassPort as ID, a.Language, a.ConfirmationNo, a.OriginCode, s.Description as SourceCode, a.MarketCode, p.FullAccount, FD.RoomID, FD.RoomType,
				a.NoOfNight, a.RateCode, a.AgentName, p.VIPReason, a.Rate,  a.MemberNo CRSNo,

				(select sum(FD.AmountBeforeTax) from FolioDetail FD inner join Reservation R on FD.ReservationID=R.ID
				inner join Profile Pf on R.ProfileIndividualID=Pf.ID where FD.GroupCode='Room' and FD.FolioID=F.ID and FD.CurrencyID='VND' and RowState = PostType) as RoomRevenueLocal, 
				
				(select sum(FD.AmountBeforeTax) from FolioDetail FD inner join Reservation R on FD.ReservationID=R.ID
				inner join Profile Pf on R.ProfileIndividualID=Pf.ID where FD.GroupCode='F&B' and FD.FolioID=F.ID and FD.CurrencyID='VND' and RowState =  1 and IsSplit=1) as FBRevenueLocal, 
				
				isnull((select sum(FD.AmountBeforeTax) from FolioDetail FD inner join Reservation R on FD.ReservationID=R.ID
				inner join Profile Pf on R.ProfileIndividualID=Pf.ID where FD.GroupCode='Room' and FD.FolioID=F.ID and FD.CurrencyID='VND' and RowState = PostType) ,0) + 
				isnull((select sum(FD.AmountBeforeTax) from FolioDetail FD inner join Reservation R on FD.ReservationID=R.ID
				inner join Profile Pf on R.ProfileIndividualID=Pf.ID where FD.GroupCode='F&B' and FD.FolioID=F.ID and FD.CurrencyID='VND' and RowState =  1 and IsSplit=1),0)
				as TotalRevenueLocal,

				(select sum(FD.AmountBeforeTax) from FolioDetail FD inner join Reservation R on FD.ReservationID=R.ID
				inner join Profile Pf on R.ProfileIndividualID=Pf.ID where FD.GroupCode='Room' and FD.FolioID=F.ID and FD.CurrencyID='USD'  and RowState = PostType) as RoomRevenueUSD,
				
				(select sum(FD.AmountBeforeTax) from FolioDetail FD inner join Reservation R on FD.ReservationID=R.ID
				inner join Profile Pf on R.ProfileIndividualID=Pf.ID where FD.GroupCode='F&B' and FD.FolioID=F.ID and FD.CurrencyID='USD' and RowState =  1 and IsSplit=1) as FBRevenueUSD, 
				
				isnull((select sum(FD.AmountBeforeTax) from FolioDetail FD inner join Reservation R on FD.ReservationID=R.ID
				inner join Profile Pf on R.ProfileIndividualID=Pf.ID where FD.GroupCode='Room' and FD.FolioID=F.ID and FD.CurrencyID='USD'  and RowState = PostType),0) +
				isnull((select sum(FD.AmountBeforeTax) from FolioDetail FD inner join Reservation R on FD.ReservationID=R.ID
				inner join Profile Pf on R.ProfileIndividualID=Pf.ID where FD.GroupCode='F&B' and FD.FolioID=F.ID and FD.CurrencyID='USD' and RowState =  1 and IsSplit=1),0)
				 as TotalRevenueUSD,
				
				FD.CurrencyMaster, 
				(select top 1 E.ExChangeRate from ExchangeRate E, Reservation R 
				where DATEDIFF(day,E.DateTime,R.CheckOutDate)=0 and E.FromCurrencyID='USD' order by E.DateTime desc ) as ExChangeRate

		FROM    dbo.Profile p INNER JOIN
				--dbo.Title t on t.id = p.TitleID INNER JOIN
				dbo.Reservation a WITH (NOLOCK) ON p.ID = a.ProfileIndividualID 
				INNER JOIN dbo.Folio F WITH (NOLOCK) ON F.ReservationID= a.ID
				INNER JOIN dbo.FolioDetail FD WITH (NOLOCK) ON FD.FolioID=F.ID
				LEFT JOIN dbo.Country ON p.CountryID = dbo.Country.ID 
				LEFT JOIN dbo.Nationality ON p.NationalityID = dbo.Nationality.ID
				INNER JOIN dbo.Source s WITH(NOLOCK) ON a.SourceID = s.ID

		WHERE    a.Status IN (1,6)
		And RoomNo >0 and a.ReservationNo>0 AND a.RoomType <> 'DMR' 

		 
	)
	SELECT	a.*
	FROM	tbRsv a 
	where a._row = 1
END
GO
