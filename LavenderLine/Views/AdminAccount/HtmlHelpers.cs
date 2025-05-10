using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text;

namespace LavenderLine.Views.AdminAccount
{
    public static class HtmlHelpers
    {
        public static IHtmlContent RoleDropdown(this IHtmlHelper htmlHelper, string selectedRole)
        {
            var sb = new StringBuilder();
            sb.Append("<select name='Role' class='form-select'>");
            sb.Append($"<option value=''>Select Role</option>");
            sb.Append($"<option value='{Roles.Admin}' {(selectedRole == Roles.Admin ? "selected" : "")}>Admin</option>");
            sb.Append($"<option value='{Roles.Customer}' {(selectedRole == Roles.Customer ? "selected" : "")}>Customer</option>");
            sb.Append($"<option value='{Roles.Manager}' {(selectedRole == Roles.Manager ? "selected" : "")}>Manager</option>");
            sb.Append("</select>");

            return new HtmlString(sb.ToString());
        }
    }
}
