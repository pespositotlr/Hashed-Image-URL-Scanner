using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashedImageURLScanner.Entities
{
    public class ProductServerData
    {

        public string Version { get; set; }
        public string S3_key { get; set; }
        public string Title { get; set; }
        public string Image_Reflow { get; set; }
        public string Page_Arrangement { get; set; }
        public string Author { get; set; }
        public string Page_Count { get; set; }
        public string Direction { get; set; }
        public string Has_Cover { get; set; }
        public string[] Links { get; set; }
        public string[] Navigation { get; set; }
        public SpineData[] Spine { get; set; }
        public string Content_Type { get; set; }
        public string[] Extra { get; set; }
    }

    public class SpineData
    {
        public string Path { get; set; }
        public string Linear { get; set; }
        public string Width { get; set; }
        public string Height { get; set; }
    }

}
