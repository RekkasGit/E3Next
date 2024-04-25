using E3NextConfigEditor.Client;
using MonoCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace E3NextConfigEditor.MQ
{
	public class MQClient : MonoCore.IMQ
	{
		public static DealerClient _tloClient;
		public MQClient(DealerClient dealer) 
		{
			_tloClient= dealer;
		
		}

		public bool AddCommand(string query)
		{
			throw new NotImplementedException();
		}

		public void ClearCommands()
		{
			throw new NotImplementedException();
		}

		public void Cmd(string query, bool delayed = false)
		{
			throw new NotImplementedException();
		}

		public void Cmd(string query, int delay, bool delayed = false)
		{
			throw new NotImplementedException();
		}

		public void Delay(int value)
		{
			throw new NotImplementedException();
		}

		public bool Delay(int maxTimeToWait, string Condition)
		{
			throw new NotImplementedException();
		}

		public bool Delay(int maxTimeToWait, Func<bool> methodToCheck)
		{
			throw new NotImplementedException();
		}

		public bool FeatureEnabled(MQFeature feature)
		{
			throw new NotImplementedException();
		}

		public string GetFocusedWindowName()
		{
			throw new NotImplementedException();
		}

		public T Query<T>(string query)
		{
			string mqReturnValue = _tloClient.RequestData(query);
			Debug.WriteLine($"[{System.DateTime.Now.ToString()}] {mqReturnValue}");

			if (typeof(T) == typeof(Int32))
			{
				if (!mqReturnValue.Contains("."))
				{
					Int32 value;
					if (Int32.TryParse(mqReturnValue, out value))
					{
						return (T)(object)value;
					}
					else { return (T)(object)-1; }
				}
				else
				{
					Decimal value;
					if (decimal.TryParse(mqReturnValue, out value))
					{
						return (T)(object)value;
					}
					else { return (T)(object)-1; }

				}
			}
			else if (typeof(T) == typeof(Boolean))
			{
				Boolean booleanValue;
				if (Boolean.TryParse(mqReturnValue, out booleanValue))
				{
					return (T)(object)booleanValue;
				}
				if (mqReturnValue == "NULL")
				{
					return (T)(object)false;
				}
				if (mqReturnValue == "!FALSE")
				{
					return (T)(object)true;
				}
				if (mqReturnValue == "!TRUE")
				{
					return (T)(object)false;
				}
				Int32 intValue;
				if (Int32.TryParse(mqReturnValue, out intValue))
				{
					if (intValue > 0)
					{
						return (T)(object)true;
					}
					return (T)(object)false;
				}
				if (string.IsNullOrWhiteSpace(mqReturnValue))
				{
					return (T)(object)false;
				}

				return (T)(object)true;


			}
			else if (typeof(T) == typeof(string))
			{
				return (T)(object)mqReturnValue;
			}
			else if (typeof(T) == typeof(decimal))
			{
				Decimal value;
				if (Decimal.TryParse(mqReturnValue, out value))
				{
					return (T)(object)value;
				}
				else { return (T)(object)-1M; }
			}
			else if (typeof(T) == typeof(double))
			{
				double value;
				if (double.TryParse(mqReturnValue, out value))
				{
					return (T)(object)value;
				}
				else { return (T)(object)-1D; }
			}
			else if (typeof(T) == typeof(Int64))
			{
				Int64 value;
				if (Int64.TryParse(mqReturnValue, out value))
				{
					return (T)(object)value;
				}
				else { return (T)(object)-1L; }
			}


			return default(T);
		}

		public void RemoveCommand(string commandName)
		{
			throw new NotImplementedException();
		}

		public void TraceEnd(string methodName)
		{
			throw new NotImplementedException();
		}

		public void TraceStart(string methodName)
		{
			throw new NotImplementedException();
		}

		public void Write(string query, [CallerMemberName] string memberName = "", [CallerFilePath] string fileName = "", [CallerLineNumber] int lineNumber = 0)
		{
			
		}
	}
}
