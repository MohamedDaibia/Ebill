namespace EBill.Helpers
{
    // Converts a rupee amount into words using the Indian numbering system
    // (Crore / Lakh / Thousand), e.g. 812501.50 -> "Indian Rupee Eight Lakh
    // Twelve Thousand Five Hundred One and Fifty Paise Only".
    public static class NumberToWords
    {
        private static readonly string[] Ones =
        {
            "", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten",
            "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen"
        };

        private static readonly string[] Tens =
        {
            "", "", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety"
        };

        public static string ConvertAmountToWords(decimal amount)
        {
            amount = Math.Round(amount, 2);
            var rupees = (long)Math.Floor(amount);
            var paise = (int)Math.Round((amount - rupees) * 100);

            var rupeeWords = rupees == 0 ? "Zero" : ConvertToWords(rupees);
            var result = $"{rupeeWords} Indian Rupee ";

            if (paise > 0)
            {
                result += $" and {ConvertBelowHundred(paise)} Paise";
            }

            return result + " Only";
        }

        private static string ConvertToWords(long number)
        {
            var parts = new List<string>();

            var crore = number / 10000000;
            number %= 10000000;
            var lakh = number / 100000;
            number %= 100000;
            var thousand = number / 1000;
            number %= 1000;
            var hundred = number / 100;
            number %= 100;

            if (crore > 0) parts.Add(ConvertBelowThousand(crore) + " Crore");
            if (lakh > 0) parts.Add(ConvertBelowThousand(lakh) + " Lakh");
            if (thousand > 0) parts.Add(ConvertBelowThousand(thousand) + " Thousand");
            if (hundred > 0) parts.Add(Ones[hundred] + " Hundred");
            if (number > 0) parts.Add(ConvertBelowHundred(number));

            return string.Join(" ", parts);
        }

        private static string ConvertBelowThousand(long number)
        {
            if (number >= 100)
            {
                var remainder = number % 100;
                return Ones[number / 100] + " Hundred" + (remainder > 0 ? " " + ConvertBelowHundred(remainder) : "");
            }

            return ConvertBelowHundred(number);
        }

        private static string ConvertBelowHundred(long number)
        {
            if (number < 20)
            {
                return Ones[number];
            }

            var remainder = number % 10;
            return Tens[number / 10] + (remainder > 0 ? " " + Ones[remainder] : "");
        }
    }
}
