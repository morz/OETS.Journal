namespace OETS.Shared.Util
{
	/// <summary>
	/// Interface for an object pool.
	/// </summary>
	/// <seealso cref="zzObjectPoolMgr"/>
	/// <remarks>
	/// An object pool holds reusable objects. See <see cref="zzObjectPoolMgr"/> for more details.
	/// </remarks>
	public interface IObjectPool
	{
		/// <summary>
		/// Amount of available objects in pool
		/// </summary>
		int AvailableCount
		{
			get;
		}

		/// <summary>
		/// Amount of objects that have been obtained but not recycled.
		/// </summary>
		int ObtainedCount
		{
			get;
		}

		/// <summary>
		/// Enqueues an object in the pool to be reused.
		/// </summary>
		/// <param name="obj">The object to be put back in the pool.</param>
		void Recycle(object obj);

		/// <summary>
		/// Grabs an object from the pool.
		/// </summary>
		/// <returns>An object from the pool.</returns>
		object ObtainObj();
	}
}