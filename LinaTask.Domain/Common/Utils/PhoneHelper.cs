using System.Text.RegularExpressions;

namespace LinaTask.Domain.Common.Utils
{
    public static class PhoneHelper
    {
        public static string NormalizeColombianPhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                throw new ArgumentException("Phone number is empty");

            // eliminar espacios, guiones, paréntesis
            phone = Regex.Replace(phone, @"[^\d+]", "");

            // si viene con 57 sin +
            if (phone.StartsWith("57") && !phone.StartsWith("+57"))
                phone = "+" + phone;

            // si viene local (3xxxxxxxxx)
            if (phone.StartsWith("3") && phone.Length == 10)
                phone = "+57" + phone;

            // si viene con 0 inicial
            if (phone.StartsWith("03"))
                phone = "+57" + phone.Substring(1);

            // validar formato final
            if (!Regex.IsMatch(phone, @"^\+573\d{9}$"))
                throw new Exception("Formato de celular colombiano inválido");

            return phone;
        }
    }
}
