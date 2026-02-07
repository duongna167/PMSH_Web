using BaseBusiness.bc;
using BaseBusiness.BO;
using BaseBusiness.Facade;
using BaseBusiness.Model;
using BaseBusiness.util;
using Microsoft.Data.SqlClient;

namespace BaseBusiness.BO
{
    public class CardBO : BaseBO
    {
        private CardFacade facade = CardFacade.Instance;
        protected static CardBO instance = new CardBO();

        protected CardBO()
        {
            this.baseFacade = facade;
        }

        public static CardBO Instance
        {
            get { return instance; }
        }

        /// <summary>
        /// Check trùng Card ID (ID là string)
        /// </summary>
        public bool IsDuplicateCardId(string cardId)
        {
            if (string.IsNullOrWhiteSpace(cardId))
                return false;

            return IsDuplicateCode(
                table: "Card",
                codeField: "ID",
                codeValue: cardId.Trim()
            );
        }

        /// <summary>
        /// Insert Card với ID là string (không auto increment)
        /// </summary>
        public void InsertCard(CardModel model)
        {
            using var conn = new SqlConnection(DBUtils.GetDBConnectionString());
            conn.Open();

            string sql = @"
                INSERT INTO Card
                (
                    ID,
                    CardTypeID,
                    Status,
                    CanSell,
                    CreatedBy,
                    CreatedDate,
                    UpdatedBy,
                    UpdatedDate
                )
                VALUES
                (
                    @ID,
                    @CardTypeID,
                    @Status,
                    @CanSell,
                    @CreatedBy,
                    @CreatedDate,
                    @UpdatedBy,
                    @UpdatedDate
                )";

            using var cmd = new SqlCommand(sql, conn);

            cmd.Parameters.AddWithValue("@ID", model.ID);
            cmd.Parameters.AddWithValue("@CardTypeID", model.CardTypeID);
            cmd.Parameters.AddWithValue("@Status", model.Status);
            cmd.Parameters.AddWithValue("@CanSell", model.CanSell);
            cmd.Parameters.AddWithValue("@CreatedBy", model.CreatedBy ?? "");
            cmd.Parameters.AddWithValue("@CreatedDate", model.CreatedDate);
            cmd.Parameters.AddWithValue("@UpdatedBy", model.UpdatedBy ?? "");
            cmd.Parameters.AddWithValue("@UpdatedDate", model.UpdatedDate);

            cmd.ExecuteNonQuery();
        }

    }
}
