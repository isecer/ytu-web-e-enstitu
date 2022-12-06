using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LisansUstuBasvuruSistemi.Utilities.Helpers
{
    public static class EvaluteExpressionHelper
    {


        // Stores user-entered primitives like X = 10.
        public static Dictionary<string, string> Primatives;

        public enum Precedence
        {
            None = 11,
            Unary = 10,     // Not actually used.
            Power = 9,      // We use ^ to mean exponentiation.
            Times = 8,
            Div = 7,
            Modulus = 6,
            Plus = 5,
        }


        // Evaluate the expression.
        public static double EvaluateExpression(this string expression)
        {
            int best_pos = 0;
            int parens = 0;

            // Remove all spaces.
            string expr = expression.Replace(" ", "");
            int expr_len = expr.Length;
            if (expr_len == 0) return 0;

            // If we find + or - now, then it's a unary operator.
            bool is_unary = true;

            // So far we have nothing.
            Precedence best_prec = Precedence.None;

            // Find the operator with the lowest precedence.
            // Look for places where there are no open
            // parentheses.
            for (int pos = 0; pos < expr_len; pos++)
            {
                // Examine the next character.
                string ch = expr.Substring(pos, 1);

                // Assume we will not find an operator. In
                // that case, the next operator will not
                // be unary.
                bool next_unary = false;

                if (ch == " ")
                {
                    // Just skip spaces. We keep them here
                    // to make the error messages easier to
                }
                else if (ch == "(")
                {
                    // Increase the open parentheses count.
                    parens += 1;

                    // A + or - after "(" is unary.
                    next_unary = true;
                }
                else if (ch == ")")
                {
                    // Decrease the open parentheses count.
                    parens -= 1;

                    // An operator after ")" is not unary.
                    next_unary = false;

                    // If parens < 0, too many )'s.
                    if (parens < 0)
                        throw new FormatException(
                            "Too many close parentheses in '" +
                            expression + "'");
                }
                else if (parens == 0)
                {
                    // See if this is an operator.
                    if ((ch == "^") || (ch == "*") ||
                        (ch == "/") || (ch == "\\") ||
                        (ch == "%") || (ch == "+") ||
                        (ch == "-"))
                    {
                        // An operator after an operator
                        // is unary.
                        next_unary = true;

                        // See if this operator has higher
                        // precedence than the current one.
                        switch (ch)
                        {
                            case "^":
                                if (best_prec >= Precedence.Power)
                                {
                                    best_prec = Precedence.Power;
                                    best_pos = pos;
                                }
                                break;

                            case "*":
                            case "/":
                                if (best_prec >= Precedence.Times)
                                {
                                    best_prec = Precedence.Times;
                                    best_pos = pos;
                                }
                                break;

                            case "%":
                                if (best_prec >= Precedence.Modulus)
                                {
                                    best_prec = Precedence.Modulus;
                                    best_pos = pos;
                                }
                                break;

                            case "+":
                            case "-":
                                // Ignore unary operators
                                // for now.
                                if ((!is_unary) &&
                                    best_prec >= Precedence.Plus)
                                {
                                    best_prec = Precedence.Plus;
                                    best_pos = pos;
                                }
                                break;
                        } // End switch (ch)
                    } // End if this is an operator.
                } // else if (parens == 0)

                is_unary = next_unary;
            } // for (int pos = 0; pos < expr_len; pos++)

            // If the parentheses count is not zero,
            // there's a ) missing.
            if (parens != 0)
            {
                throw new FormatException(
                    "Missing close parenthesis in '" +
                    expression + "'");
            }

            // Hopefully we have the operator.
            if (best_prec < Precedence.None)
            {
                string lexpr = expr.Substring(0, best_pos);
                string rexpr = expr.Substring(best_pos + 1);
                switch (expr.Substring(best_pos, 1))
                {
                    case "^":
                        return Math.Pow(
                            EvaluateExpression(lexpr),
                            EvaluateExpression(rexpr));
                    case "*":
                        return
                            EvaluateExpression(lexpr) *
                            EvaluateExpression(rexpr);
                    case "/":
                        return
                            EvaluateExpression(lexpr) /
                            EvaluateExpression(rexpr);
                    case "%":
                        return
                            EvaluateExpression(lexpr) %
                            EvaluateExpression(rexpr);
                    case "+":
                        return
                            EvaluateExpression(lexpr) +
                            EvaluateExpression(rexpr);
                    case "-":
                        return
                            EvaluateExpression(lexpr) -
                            EvaluateExpression(rexpr);
                }
            }

            // if we do not yet have an operator, there
            // are several possibilities:
            //
            // 1. expr is (expr2) for some expr2.
            // 2. expr is -expr2 or +expr2 for some expr2.
            // 3. expr is Fun(expr2) for a function Fun.
            // 4. expr is a primitive.
            // 5. It's a literal like "3.14159".

            // Look for (expr2).
            if (expr.StartsWith("(") && expr.EndsWith(")"))
            {
                // Remove the parentheses.
                return EvaluateExpression(expr.Substring(1, expr_len - 2));
            }

            // Look for -expr2.
            if (expr.StartsWith("-"))
            {
                return -EvaluateExpression(expr.Substring(1));
            }

            // Look for +expr2.
            if (expr.StartsWith("+"))
            {
                return EvaluateExpression(expr.Substring(1));
            }

            // Look for Fun(expr2).
            if (expr_len > 5 && expr.EndsWith(")"))
            {
                // Find the first (.
                int paren_pos = expr.IndexOf("(");
                if (paren_pos > 0)
                {
                    // See what the function is.
                    string lexpr = expr.Substring(0, paren_pos);
                    string rexpr = expr.Substring(paren_pos + 1, expr_len - paren_pos - 2);
                    switch (lexpr.ToLower())
                    {
                        case "sin":
                            return Math.Sin(EvaluateExpression(rexpr));
                        case "cos":
                            return Math.Cos(EvaluateExpression(rexpr));
                        case "tan":
                            return Math.Tan(EvaluateExpression(rexpr));
                        case "sqrt":
                            return Math.Sqrt(EvaluateExpression(rexpr));
                        case "factorial":
                            return Factorial(EvaluateExpression(rexpr));
                            // Add other functions (including
                            // program-defined functions) here.
                    }
                }
            }
            Primatives = new Dictionary<string, string>();
            Primatives.Add("Pi", "3.14159265");
            // See if it's a primitive.
            if (Primatives.ContainsKey(expr))
            {
                // Return the corresponding value,
                // converted into a Double.
                try
                {
                    // Try to convert the expression into a value.
                    return double.Parse(Primatives[expr]);
                }
                catch (Exception)
                {
                    throw new FormatException(
                        "Primative '" + expr +
                        "' has value '" +
                        Primatives[expr] +
                        "' which is not a Double.");
                }
            }

            // It must be a literal like "2.71828".
            try
            {
                // Try to convert the expression into a Double.
                return double.Parse(expr);
            }
            catch (Exception)
            {
                throw new FormatException(
                    "Error evaluating '" + expression +
                    "' as a constant.");
            }
        }

        // Return the factorial of the expression.
        public static double Factorial(double value)
        {
            // Make sure the value is an integer.
            if ((long)value != value)
            {
                throw new ArgumentException(
                    "Parameter to Factorial function must be an integer in Factorial(" +
                    value.ToString() + ")");
            }

            double result = 1;
            for (int i = 2; i <= value; i++)
            {
                result *= i;
            }
            return result;
        }
    }
}