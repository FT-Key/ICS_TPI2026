function Input({ label, error = '', ...restProps }) {
  return (
    <div
      className='
        flex
        flex-col
        h-20
      '
    >
      <label
      className="text-sm sm:text-base">{label}:</label>
      <input className={`
      text-base sm:text-lg 
      p-2 sm:p-3
      ${error && 'border-red-400'}
      `} 
  {...restProps}/>
      {error && <p className="text-red-500 text-base sm:text-xs">{error}</p>}
    </div>
  );
};

export default Input;
