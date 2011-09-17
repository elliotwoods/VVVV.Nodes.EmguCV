﻿using System;
using System.Collections.Generic;
using VVVV.PluginInterfaces.V2;

namespace VVVV.Nodes.EmguCV
{
	public abstract class ImageProcessingNode<T>: IPluginEvaluate, IDisposable where T : ImageProcessingInstance, new()
	{
		protected Dictionary<int, T> InstancesByIndex = new Dictionary<int, T>();
		
		public virtual void Evaluate(int SpreadMax)
		{
			CheckInctancesSize(SpreadMax);
		}

		public virtual void CheckInctancesSize(int spreadMax)
		{
			if (InstancesByIndex.Count < spreadMax)
			{
				for (int i = 0; i < spreadMax; i++)
				{
					if (!InstancesByIndex.ContainsKey(i))
					{
						InstancesByIndex.Add(i, new T());
					}
				}
			}
			else if (InstancesByIndex.Count > spreadMax) return;

			for (int i = spreadMax; i < InstancesByIndex.Count; i++)
			{
				InstancesByIndex.Remove(i);
			}
		}

		public virtual void Dispose()
		{
			foreach (KeyValuePair<int, T> keyValuePair in InstancesByIndex)
			{
				keyValuePair.Value.Close();
			}
		}
	}
}