using System.Text;

namespace KartRider.Utilities;

public static class RTTIUtilities
{
	public static uint GenerateRTTIHash(string str, uint a1 = 0u)
	{
		return GenerateRTTIHash(Encoding.Unicode.GetBytes(str), a1);
	}

	public static uint GenerateRTTIHash(byte[] str, uint a1 = 0u)
	{
		int v9 = 0;
		uint v10 = (uint)str.Length;
		uint v11 = a1 >> 16;
		uint v12 = (ushort)a1;
		if (str.Length == 1)
		{
			int v13 = (int)(str[0] + v12);
			if ((uint)v13 >= 65521u)
			{
				v13 -= 65521;
			}
			int v14 = (int)(v13 + v11);
			if ((uint)v14 >= 65521u)
			{
				v14 -= 65521;
			}
			return (uint)(v13 | (v14 << 16));
		}
		if (str != null)
		{
			if (str.Length >= 16)
			{
				if (str.Length >= 5552)
				{
					uint v15 = (uint)str.Length / 5552u;
					do
					{
						v10 -= 5552;
						int v16 = 347;
						do
						{
							int v17 = (int)(str[v9] + v12);
							int v18 = v17 + (int)v11;
							int v19 = str[v9 + 1] + v17;
							int v20 = v19 + v18;
							int v21 = str[v9 + 2] + v19;
							int v22 = v21 + v20;
							int v23 = str[v9 + 3] + v21;
							int v24 = v23 + v22;
							int v25 = str[v9 + 4] + v23;
							int v26 = v25 + v24;
							int v27 = str[v9 + 5] + v25;
							int v28 = v27 + v26;
							int v29 = str[v9 + 6] + v27;
							int v30 = v29 + v28;
							int v31 = str[v9 + 7] + v29;
							int v32 = v31 + v30;
							int v33 = str[v9 + 8] + v31;
							int v34 = v33 + v32;
							int v35 = str[v9 + 9] + v33;
							int v36 = v35 + v34;
							int v37 = str[v9 + 10] + v35;
							int v38 = v37 + v36;
							int v39 = str[v9 + 11] + v37;
							int v40 = v39 + v38;
							int v41 = str[v9 + 12] + v39;
							int v42 = v41 + v40;
							int v43 = str[v9 + 13] + v41;
							int v44 = v43 + v42;
							int v45 = str[v9 + 14] + v43;
							int v46 = v45 + v44;
							v12 = (uint)(str[v9 + 15] + v45);
							v11 = v12 + (uint)v46;
							v9 += 16;
							v16--;
						}
						while (v16 != 0);
						v12 %= 65521;
						v15--;
						v11 %= 65521;
					}
					while (v15 != 0);
				}
				if (v10 != 0)
				{
					if (v10 >= 16)
					{
						uint v47 = v10 >> 4;
						do
						{
							int v48 = (int)(str[v9] + v12);
							int v49 = v48 + (int)v11;
							int v50 = str[v9 + 1] + v48;
							int v51 = v50 + v49;
							int v52 = str[v9 + 2] + v50;
							int v53 = v52 + v51;
							int v54 = str[v9 + 3] + v52;
							int v55 = v54 + v53;
							int v56 = str[v9 + 4] + v54;
							int v57 = v56 + v55;
							int v58 = str[v9 + 5] + v56;
							int v59 = v58 + v57;
							int v60 = str[v9 + 6] + v58;
							int v61 = v60 + v59;
							int v62 = str[v9 + 7] + v60;
							int v63 = v62 + v61;
							int v64 = str[v9 + 8] + v62;
							int v65 = v64 + v63;
							int v66 = str[v9 + 9] + v64;
							int v67 = v66 + v65;
							int v68 = str[v9 + 10] + v66;
							int v69 = v68 + v67;
							int v70 = str[v9 + 11] + v68;
							int v71 = v70 + v69;
							int v72 = str[v9 + 12] + v70;
							int v73 = v72 + v71;
							int v74 = str[v9 + 13] + v72;
							int v75 = v74 + v73;
							int v76 = str[v9 + 14] + v74;
							int v77 = v76 + v75;
							v12 = (uint)(str[v9 + 15] + v76);
							v10 -= 16;
							v11 = v12 + (uint)v77;
							v9 += 16;
							v47--;
						}
						while (v47 != 0);
					}
					while (v10 != 0)
					{
						v12 += str[v9++];
						v11 += v12;
						v10--;
					}
					v12 %= 65521;
					v11 %= 65521;
				}
				return v12 | (v11 << 16);
			}
			if (str.Length != 0)
			{
				do
				{
					v12 += str[v9++];
					v11 += v12;
					v10--;
				}
				while (v10 != 0);
			}
			if (v12 >= 65521)
			{
				v12 -= 65521;
			}
			return v12 | (v11 % 65521 << 16);
		}
		return 1u;
	}
}
