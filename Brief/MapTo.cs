using System;

namespace Brief {
    public class MapTo : Attribute {

        public MapTo(string column) {
            MapColumn = column;
        }

        public string MapColumn { set; get; }


    }
}
