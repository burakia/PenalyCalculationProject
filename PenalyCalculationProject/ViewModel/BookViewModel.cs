using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace PenalyCalculationProject.ViewModel
{
    public class BookViewModel
    {
        [Key]
        public int bookId { get; set; }

        [Display(Name = "Book Name")]
        [Required(ErrorMessage = "Required")]
        public string bookName { get; set; }
        
        [Required(ErrorMessage = "Required")]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{yyyy/MM/dd}")]
        [Display(Name = "Checked Out Date")]
        [DataType(DataType.Date)]
        public DateTime checkedOutDate { get; set; }
        
        [Required(ErrorMessage = "Required")]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{yyyy/MM/dd}")]
        [Display(Name = "Returned Date")]
        [DataType(DataType.Date)]
        public DateTime returnedDate { get; set; }

        [Display(Name = "Country")]
        public int countryId { get; set; }

        public string currency { get; set; }

        public string currencyDesc { get; set; }



    }
}