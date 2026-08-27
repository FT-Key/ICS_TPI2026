// Este archivo solo define cómo se ve UNA tarjeta.
export default function Card({ children, className = "" }) {
  return (
    <div className={`
      bg-white 
      border-2 border-gray-200  /* El estilo borde negro que querías */
      p-4 
      rounded-xl 
      shadow-sm
      ${className}
    `}>
      {children}
    </div>
  );
}