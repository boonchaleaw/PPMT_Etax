using System.Collections.Generic;

namespace Etax_Api.Class.MocApi
{
    public class WalkinCertData
    {
        public string refID = "";
        public string juristicID = "";
        public string juristicNameTH = "";
        public string juristicNameEN = "";
        public string juristicType = "";
        public string registerDate = "";
        public string juristicStatus = "";
        public string registerCapital = "";
        public string standardObjective = "";
        public List<Director> directors = new List<Director>();
        public StandardObjectiveDetail standardObjectiveDetail = new StandardObjectiveDetail();
        public AddressDetail addressDetail = new AddressDetail();
    }

    public class StandardObjectiveDetail
    {
        public string objectiveDescription = "";
    }
    public class AddressDetail
    {
        public string addressFull = "";
        public string addressName = "";
        public string buildingName = "";
        public string roomNo = "";
        public string floor = "";
        public string villageName = "";
        public string houseNumber = "";
        public string moo = "";
        public string soi = "";
        public string street = "";
        public string subDistrict = "";
        public string district = "";
        public string province = "";
    }

    public class Director
    {
        public string name = "";
    }
}
