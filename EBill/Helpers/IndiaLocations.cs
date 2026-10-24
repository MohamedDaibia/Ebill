namespace EBill.Helpers
{
    // States / union territories of India and their important districts, used by the
    // State -> District dropdowns on the customer forms. Tamil Nadu is complete (all 38
    // districts); the other states list their major districts only.
    public static class IndiaLocations
    {
        public static readonly IReadOnlyDictionary<string, string[]> StateDistricts =
            new SortedDictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["Tamil Nadu"] = new[]
                {
                    "Ariyalur", "Chengalpattu", "Chennai", "Coimbatore", "Cuddalore", "Dharmapuri",
                    "Dindigul", "Erode", "Kallakurichi", "Kancheepuram", "Kanniyakumari", "Karur",
                    "Krishnagiri", "Madurai", "Mayiladuthurai", "Nagapattinam", "Namakkal", "Nilgiris",
                    "Perambalur", "Pudukkottai", "Ramanathapuram", "Ranipet", "Salem", "Sivagangai",
                    "Tenkasi", "Thanjavur", "Theni", "Thoothukudi", "Tiruchirappalli", "Tirunelveli",
                    "Tirupathur", "Tiruppur", "Tiruvallur", "Tiruvannamalai", "Tiruvarur", "Vellore",
                    "Viluppuram", "Virudhunagar"
                },
                ["Andhra Pradesh"] = new[] { "Anantapur", "Chittoor", "East Godavari", "Guntur", "Krishna", "Kurnool", "Nellore", "Prakasam", "Srikakulam", "Visakhapatnam", "Vizianagaram", "West Godavari", "YSR Kadapa" },
                ["Arunachal Pradesh"] = new[] { "Itanagar (Papum Pare)", "Lower Subansiri", "Tawang", "West Kameng", "East Siang" },
                ["Assam"] = new[] { "Cachar", "Dibrugarh", "Jorhat", "Kamrup", "Kamrup Metropolitan (Guwahati)", "Nagaon", "Silchar", "Sonitpur", "Tinsukia" },
                ["Bihar"] = new[] { "Bhagalpur", "Darbhanga", "Gaya", "Muzaffarpur", "Nalanda", "Patna", "Purnia", "Saran", "Vaishali" },
                ["Chhattisgarh"] = new[] { "Bastar", "Bilaspur", "Durg", "Korba", "Raigarh", "Raipur", "Rajnandgaon" },
                ["Goa"] = new[] { "North Goa", "South Goa" },
                ["Gujarat"] = new[] { "Ahmedabad", "Anand", "Bhavnagar", "Gandhinagar", "Jamnagar", "Junagadh", "Kutch", "Mehsana", "Rajkot", "Surat", "Vadodara"},
                ["Haryana"] = new[] { "Ambala", "Faridabad", "Gurugram", "Hisar", "Karnal", "Panipat", "Rohtak", "Sonipat", "Yamunanagar" },
                ["Himachal Pradesh"] = new[] { "Bilaspur", "Chamba", "Kangra", "Kullu", "Mandi", "Shimla", "Solan" },
                ["Jharkhand"] = new[] { "Bokaro", "Deoghar", "Dhanbad", "East Singhbhum (Jamshedpur)", "Hazaribagh", "Ranchi" },
                ["Karnataka"] = new[] { "Belagavi", "Bengaluru Rural", "Bengaluru Urban", "Dakshina Kannada (Mangaluru)", "Dharwad", "Kalaburagi", "Mysuru", "Shivamogga", "Tumakuru", "Udupi" },
                ["Kerala"] = new[] { "Alappuzha", "Ernakulam", "Idukki", "Kannur", "Kasaragod", "Kollam", "Kottayam", "Kozhikode", "Malappuram", "Palakkad", "Pathanamthitta", "Thiruvananthapuram", "Thrissur", "Wayanad" },
                ["Madhya Pradesh"] = new[] { "Bhopal", "Gwalior", "Indore", "Jabalpur", "Rewa", "Sagar", "Satna", "Ujjain" },
                ["Maharashtra"] = new[] { "Ahmednagar", "Aurangabad (Chhatrapati Sambhajinagar)", "Kolhapur", "Mumbai City", "Mumbai Suburban", "Nagpur", "Nashik", "Pune", "Satara", "Solapur", "Thane" },
                ["Manipur"] = new[] { "Bishnupur", "Churachandpur", "Imphal East", "Imphal West", "Thoubal" },
                ["Meghalaya"] = new[] { "East Khasi Hills (Shillong)", "Ri Bhoi", "West Garo Hills", "West Khasi Hills" },
                ["Mizoram"] = new[] { "Aizawl", "Champhai", "Kolasib", "Lunglei" },
                ["Nagaland"] = new[] { "Dimapur", "Kohima", "Mokokchung", "Wokha" },
                ["Odisha"] = new[] { "Balasore", "Cuttack", "Ganjam", "Khordha (Bhubaneswar)", "Puri", "Sambalpur", "Sundargarh" },
                ["Punjab"] = new[] { "Amritsar", "Bathinda", "Jalandhar", "Ludhiana", "Patiala", "SAS Nagar (Mohali)" },
                ["Rajasthan"] = new[] { "Ajmer", "Alwar", "Bikaner", "Jaipur", "Jodhpur", "Kota", "Udaipur" },
                ["Sikkim"] = new[] { "East Sikkim (Gangtok)", "North Sikkim", "South Sikkim", "West Sikkim" },
                ["Telangana"] = new[] { "Hyderabad", "Karimnagar", "Khammam", "Medchal-Malkajgiri", "Nizamabad", "Rangareddy", "Warangal" },
                ["Tripura"] = new[] { "Dhalai", "North Tripura", "South Tripura", "West Tripura (Agartala)" },
                ["Uttar Pradesh"] = new[] { "Agra", "Allahabad (Prayagraj)", "Ghaziabad", "Gorakhpur", "Kanpur Nagar", "Lucknow", "Meerut", "Noida (Gautam Buddh Nagar)", "Varanasi" },
                ["Uttarakhand"] = new[] { "Dehradun", "Haridwar", "Nainital", "Udham Singh Nagar" },
                ["West Bengal"] = new[] { "Howrah", "Darjeeling", "Hooghly", "Kolkata", "North 24 Parganas", "Paschim Medinipur", "South 24 Parganas" },

                // Union territories
                ["Andaman and Nicobar Islands"] = new[] { "Nicobar", "North and Middle Andaman", "South Andaman (Port Blair)" },
                ["Chandigarh"] = new[] { "Chandigarh" },
                ["Dadra and Nagar Haveli and Daman and Diu"] = new[] { "Dadra and Nagar Haveli", "Daman", "Diu" },
                ["Delhi"] = new[] { "Central Delhi", "East Delhi", "New Delhi", "North Delhi", "South Delhi", "West Delhi" },
                ["Jammu and Kashmir"] = new[] { "Anantnag", "Baramulla", "Jammu", "Srinagar" },
                ["Ladakh"] = new[] { "Kargil", "Leh" },
                ["Lakshadweep"] = new[] { "Lakshadweep (Kavaratti)" },
                ["Puducherry"] = new[] { "Karaikal", "Mahe", "Puducherry", "Yanam" }
            };
    }
}
