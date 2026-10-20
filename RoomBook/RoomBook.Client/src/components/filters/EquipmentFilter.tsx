type EquipmentFilterProps = {
  selectedEquipment: string[];
  onEquipmentChange: (equipment: string) => void;
};

const equipmentOptions = [
  { id: "projector", label: "Projector" },
  { id: "tv-screen", label: "TvScreen" },
  { id: "video-conferencing", label: "VideoConferencing" },
  { id: "whiteboard", label: "Whiteboard" },
];

function EquipmentFilter({
  selectedEquipment,
  onEquipmentChange,
}: EquipmentFilterProps) {
  return (
    <div className="col-12">
      <label className="form-label d-block">EQUIPMENT</label>

      <div className="d-flex flex-wrap gap-2">
        {equipmentOptions.map((option) => (
          <div key={option.id}>
            <input
              type="checkbox"
              className="btn-check"
              id={option.id}
              autoComplete="off"
              checked={selectedEquipment.includes(option.label)}
              onChange={() => onEquipmentChange(option.label)}
            />

            <label className="btn btn-outline-secondary" htmlFor={option.id}>
              {option.label}
            </label>
          </div>
        ))}
      </div>
    </div>
  );
}

export default EquipmentFilter;
