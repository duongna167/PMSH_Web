using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
namespace BaseBusiness.Model
{
    public class RoomUpdateDTO
    {
        public int RoomIds { get; set; } = 0;
        public int NewHKFOStatus { get; set; } = 0;
        public int UpdateByID { get; set; } = 0;
    }



}
